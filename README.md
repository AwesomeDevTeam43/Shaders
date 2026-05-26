# TAP3D — Shaders Project

Projeto Unity desenvolvido para a cadeira de TAP3D. Todos os efeitos visuais foram implementados com shaders escritos em HLSL/CG através do sistema **ShaderLab** da Unity. Alguns shaders são controlados por scripts C# que alimentam os seus parâmetros em tempo real.

---

## Índice

1. [CCTV Glitch](#1-cctv-glitch)
2. [Energy Shield](#2-energy-shield)
3. [Hologram](#3-hologram)
4. [Radioactive Liquid](#4-radioactive-liquid)
5. [Stencil Portal + X-Ray](#5-stencil-portal--x-ray)
6. [Kerr — Black Hole Volumétrico](#6-kerr--black-hole-volumétrico)
7. [Reading Steiner](#7-reading-steiner)

---

## 1. CCTV Glitch

**Ficheiros:**
- `Assets/Shaders/CCTV_Glitch.shader`
- `Assets/Scripts/CCTVGlitchEffect.cs`

### O que faz

Post-process de câmara que simula uma câmara de vigilância a falhar. Aplica três efeitos em simultâneo: **deslocamento horizontal aleatório** de linhas, **aberração cromática** e **ruído de grão**.

### Shader — Partes Importantes

O shader corre em **full-screen** (`Cull Off ZWrite Off ZTest Always`) sobre o render da câmara via `OnRenderImage`.

**Função de ruído** usada em todo o shader:
```hlsl
float random(float2 st)
{
    return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
}
```
Gerador de pseudo-aleatoriedade clássico baseado em `sin` — produz valores entre 0 e 1 a partir de qualquer coordenada 2D.

**Glitch de deslocamento horizontal:**
```hlsl
float glitchTime = random(_Time.y * 10) * random(_Time.y * 100);
if (glitchTime > 0.5 * (1.0 - _GlitchIntensity))
{
    uv.x += random(uv.y * 10 + _Time.y) * _GlitchIntensity * 0.05;
}
```
O produto de dois valores aleatórios com frequências diferentes cria "batimentos" irregulares — o deslocamento só acontece quando esse produto ultrapassa um threshold controlado por `_GlitchIntensity`.

**Aberração cromática:**
```hlsl
float4 colR = tex2D(_MainTex, uv - float2(_ChromAberration * _GlitchIntensity, 0));
float4 colG = tex2D(_MainTex, uv);
float4 colB = tex2D(_MainTex, uv + float2(_ChromAberration * _GlitchIntensity, 0));

float4 col = float4(colR.r, colG.g, colB.b, 1.0);
```
Cada canal RGB é amostrado com um offset horizontal diferente, separando as cores como numa lente com aberração.

**Scan lines + grão:**
```hlsl
float scanLine = random(uv.y * _LinesFrequency * 100 + _Time.y * 2.0);
col.rgb -= saturate(scanLine * 0.1);

float noise = random(uv + _Time.y);
float grain = (noise - 0.5) * _GraoIntensity;
col.rgb += grain * _GlitchIntensity * 0.5;
```
As scan lines são escurecimentos sutis randomizados por linha vertical. O grão é ruído branco centrado em 0 adicionado ao RGB final.

**Desaturação parcial:**
```hlsl
float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
col.rgb = lerp(col.rgb, float3(luma, luma, luma), 0.4);
```
Converte para luminância com os pesos padrão ITU-R e faz blend de 40% com a cor original, dando o aspecto lavado de CCTV.

---

### Script — `CCTVGlitchEffect.cs`

O script é um **Image Effect** (componente na câmara). Gera glitches aleatórios com timing imprevisível e amplifica o efeito quando o jogador se aproxima de um alvo.

**Glitch aleatório com timer:**
```csharp
void Update()
{
    glitchTimer -= Time.deltaTime;
    if (glitchTimer <= 0f)
    {
        // 15% de chance de glitch forte, resto usa a intensidade base
        targetGlitchValue = Random.value > 0.85f ? Random.Range(0.4f, 0.8f) : baseIntensity;
        glitchTimer = Random.Range(0.1f, 1.0f);
    }
    currentGlitchValue = Mathf.Lerp(currentGlitchValue, targetGlitchValue, Time.deltaTime * 8f);
```

**Amplificação por proximidade:**
```csharp
float proximityPercent = 1 - Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
glitchProximity = proximityPercent * distanceIntensity;
```
Quanto mais perto o jogador está do `alvo`, maior o `glitchProximity` — os dois valores somam-se para o valor final que vai para o shader.

**Aplicação ao shader via `OnRenderImage`:**
```csharp
void OnRenderImage(RenderTexture source, RenderTexture destination)
{
    Graphics.Blit(source, destination, glitchMaterial);
}
```

---

## 2. Energy Shield

**Ficheiros:**
- `Assets/Shaders/EnergyShield.shader`
- `Assets/Scripts/EnergyShieldController.cs`
- `Assets/shield_test.cs` *(script de teste/debug)*

### O que faz

Escudo energético transparente com efeito **Fresnel** nas bordas, **pulsação** sinusoidal e **onda de impacto** que se propaga radialmente na superfície quando algo coide com o escudo. Usa um **geometry shader** para deslocar fisicamente os triângulos na frente de onda.

### Shader — Partes Importantes

Este shader tem 3 estágios: `vertex → geometry → fragment`, requerendo `#pragma target 4.0`.

**Pipeline de structs:**
```hlsl
struct appdata  { float4 vertex; float3 normal; };
struct v2g      { float4 vertex; float3 worldPos; float3 normal; };
struct g2f      { float4 pos; float3 worldPos; float3 normal; float3 viewDir; };
```
O vertex shader calcula a posição no mundo mas passa o triângulo ao geometry shader antes de projetar para clip space.

**Geometry shader — expansão na frente de onda:**
```hlsl
[maxvertexcount(3)]
void geom(triangle v2g IN[3], inout TriangleStream<g2f> stream)
{
    float3 center = (IN[0].worldPos + IN[1].worldPos + IN[2].worldPos) / 3.0;
    float currentRadius = _ImpactTime * _MaxRadius;
    float dist = length(center - _ImpactPos.xyz);

    float onWave = smoothstep(_RippleWidth, 0.0, abs(dist - currentRadius));
    float expand = onWave * 0.04 * (1.0 - _ImpactTime) * active;

    // Desloca cada vértice ao longo da normal do triângulo
    float3 newWorldPos = IN[i].worldPos + faceNormal * expand;
```
O raio atual cresce de 0 a `_MaxRadius` ao longo do tempo (`_ImpactTime` de 0→1). `smoothstep` cria uma banda estreita onde `onWave > 0`. Os triângulos nessa banda são empurrados para fora pela sua normal — criando uma "bolha" 3D na superfície.

**Fragment shader — Fresnel + ripple:**
```hlsl
float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelEffect);
float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;

float rippleFade = ripple * (1.0 - _ImpactTime) * step(0.001, _ImpactTime);

col.rgb = _Color.rgb;
col.rgb += fresnel * _Color.rgb;       // bordas mais brilhantes
col.rgb += pulse * _Color.rgb * 0.1;  // pulsação subtil
col.rgb = lerp(col.rgb, _RippleColor.rgb, rippleFade); // cor da onda
col.a = _Color.a + fresnel * 0.3 + rippleFade * 0.5;
```
O efeito Fresnel (`dot(N, V)` baixo = vértice de perfil = mais brilhante) é a base visual. A onda de impacto sobrepõe-se com `_RippleColor` e aumenta a opacidade.

---

### Script — `EnergyShieldController.cs`

Deteta colisões e conduz a animação do impacto no shader via coroutine.

**Deteção de colisão:**
```csharp
void OnCollisionEnter(Collision col)
{
    TriggerImpact(col.contacts[0].point);
}

void OnTriggerEnter(Collider other)
{
    Vector3 impactPoint = GetComponent<Collider>().ClosestPoint(other.transform.position);
    TriggerImpact(impactPoint);
}
```
Suporta tanto física física (`OnCollisionEnter`) como trigger volumes (`OnTriggerEnter`).

**Animação do `_ImpactTime`:**
```csharp
public void TriggerImpact(Vector3 worldPosition, float radius = 0.6f)
{
    _mat.SetVector(ImpactPosID, new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 0f));
    if (_animCoroutine != null) StopCoroutine(_animCoroutine);
    _animCoroutine = StartCoroutine(AnimateImpact());
}

IEnumerator AnimateImpact()
{
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / impactDuration;
        _mat.SetFloat(ImpactTimeID, Mathf.Clamp01(t));
        yield return null;
    }
    _mat.SetFloat(ImpactTimeID, 0f); // reset
}
```
`_ImpactPos` diz ao shader *onde* começou o impacto. `_ImpactTime` controla quanto a onda já se expandiu. Uma nova colisão durante a animação cancela a coroutine anterior e reinicia.

### Script — `ShieldAutoTest.cs` (`shield_test.cs`)

Script de debug apenas. Dispara impactos automáticos em pontos aleatórios da superfície do escudo a cada `interval` segundos e visualiza os pontos de impacto com gizmos no Editor. Não afeta nenhuma lógica de jogo.

---

## 3. Hologram

**Ficheiros:**
- `Assets/Shaders/Hologram.shader`
- `Assets/Scripts/HologramController.cs`

### O que faz

Shader de holograma sci-fi com **efeito rim/Fresnel**, **scan lines animadas** e **glitch geométrico** que distorce os vértices horizontalmente. A intensidade do glitch aumenta conforme o jogador se aproxima do objeto.

### Shader — Partes Importantes

**Breathing (vertex shader):**
```hlsl
float breath = (sin(_Time.y * _BreathSpeed) + cos(_Time.y * _BreathSpeed * 0.8)) * 0.5;
v.vertex.xyz += v.normal * breath * _BreathAmp;
```
Soma duas oscilações com frequências ligeiramente diferentes (sin + cos) para obter movimento orgânico e não perfeitamente periódico. O vértice é deslocado ao longo da sua normal.

**Glitch geométrico (vertex shader):**
```hlsl
float slice = sin(_Time.y * 50.0 + v.vertex.y * 20.0);

float baseSnap = step(0.8, sin(_Time.y * 15.0));   // pulso on/off
float glitchSnap = max(baseSnap, _IsConstantGlitch); // ou sempre ativo

v.vertex.x += slice * glitchSnap * _GlitchIntensity * 0.1;
```
`step(0.8, ...)` cria um sinal binário (0 ou 1) que "dispara" quando o seno ultrapassa 0.8 — produz pulsos curtos irregulares. `_IsConstantGlitch = 1` força o glitch sempre ligado.

**Rim + scan lines (fragment shader):**
```hlsl
float rim = 1.0 - saturate(dot(normal, viewDir));
float rimIntensity = pow(rim, _RimPower);

float scanline = sin(i.worldPos.y * _ScanlineFreq - _Time.y * _ScanlineSpeed);
scanline = scanline * 0.5 + 0.5; // remapeia para [0, 1]

col.a = _Color.a * rimIntensity * lerp(1.0, scanline, _ScanlineIntensity);
```
O rim/Fresnel é calculado como `1 - dot(N, V)`, elevado a `_RimPower` para controlar a largura do halo. As scan lines atenuam a opacidade com uma onda sinusoidal no eixo Y do mundo — que se move para cima com `_ScanlineSpeed`.

**Flicker com glitch:**
```hlsl
float flicker = lerp(1.0, sin(_Time.y * 40.0) * 0.5 + 0.5, _GlitchIntensity * glitchSnap);
col.a *= flicker;
```
Quando o glitch está ativo, a opacidade oscila a 40Hz — criando um efeito de cintilação visível.

---

### Script — `HologramController.cs`

Aplica o material de holograma a todos os renderers filhos e controla a intensidade do glitch em função da distância ao jogador.

**Setup automático de materiais:**
```csharp
Renderer[] todosOsRenderers = GetComponentsInChildren<Renderer>();
foreach (Renderer rend in todosOsRenderers)
{
    Material[] novosMateriais = new Material[rend.materials.Length];
    for (int i = 0; i < novosMateriais.Length; i++)
    {
        Material matInstance = new Material(materialHologramaBase); // instância separada
        novosMateriais[i] = matInstance;
        materiaisInstanciados.Add(matInstance);
    }
    rend.materials = novosMateriais;
}
```
Cada slot de material recebe a sua própria instância — necessário para que os parâmetros possam ser alterados sem afetar outros objetos que usem o mesmo material base.

**Glitch por proximidade:**
```csharp
float distance = Vector3.Distance(transform.position, playerTransform.position);

float intensity = Mathf.InverseLerp(glitchStartDistance, glitchMaxDistance, distance);
float isConstant = (distance <= constantGlitchDistance) ? 1f : 0f;

foreach (Material mat in materiaisInstanciados)
{
    mat.SetFloat(glitchIntensityID, intensity);
    mat.SetFloat(isConstantGlitchID, isConstant);
}
```
`InverseLerp` normaliza a distância para [0,1] no intervalo `[glitchMaxDistance, glitchStartDistance]`. Abaixo de `constantGlitchDistance`, `_IsConstantGlitch = 1` — o glitch passa a ser contínuo.

---

## 4. Radioactive Liquid

**Ficheiro:**
- `Assets/Shaders/RadioactiveLiquid.shader`

### O que faz

Simula líquido radioativo num recipiente. Usa **três passes** no mesmo shader: vidro com Fresnel, máscara de stencil para conter o líquido, e a superfície do líquido com ondas compostas e espuma emissiva.

### Shader — Partes Importantes

**Pass 1 — Vidro (Fresnel):**
```hlsl
float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
col.a = lerp(_GlassColor.a, 0.35, fresnel);
```
Vidro quase invisível de frente, mas com reflexo nas bordas (ângulos rasantes).

**Pass 2 — Stencil Mask:**
```hlsl
// ColorMask 0 — não escreve cor nenhuma
// Cull Front — renderiza o interior do recipiente
Stencil { Ref 1; Comp Always; Pass Replace; }
```
Escreve `1` no stencil buffer nas faces internas do recipiente. O passo seguinte só renderiza onde o stencil é `1` — assim o líquido nunca aparece fora do recipiente.

**Pass 3 — Superfície do Líquido (Liquid):**
```hlsl
Stencil { Ref 1; Comp Equal; } // só pinta onde o stencil == 1
```

**Ondas compostas:**
```hlsl
float primaryWave   = sin(i.worldPos.x * _WaveFrequency + t) * _WaveAmplitude;
float secondaryWave = cos(i.worldPos.z * _WaveFrequency * 0.75 + t * 1.3) * _WaveAmplitude * _SecondaryWaveScale;
float turbulence    = sin(i.worldPos.x * _WaveFreq * 2.1 + t * 0.7)
                      * cos(i.worldPos.z * _WaveFreq * 1.8 + t * 1.1) * _WaveAmplitude * 0.3;

float noiseWave = smoothNoise(float2(worldPos.xz) * _NoiseScale + _Time.y * 0.5) * _WaveAmplitude * 2.0 * _NoiseStrength;

float totalWave = primaryWave + secondaryWave + turbulence + noiseWave;
```
Quatro componentes de onda com frequências e velocidades diferentes são somadas — o resultado é um movimento de superfície orgânico e não repetitivo.

**Função de ruído suave (Value Noise):**
```hlsl
float smoothNoise(float2 p)
{
    float2 i = floor(p); float2 f = frac(p);
    float a = hash(i), b = hash(i + float2(1,0));
    float c = hash(i + float2(0,1)), d = hash(i + float2(1,1));
    float2 u = f * f * (3.0 - 2.0 * f); // smoothstep cúbico
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}
```
Value Noise clássico: interpola entre 4 valores aleatórios vizinhos com a curva `3t²-2t³` (smoothstep) para evitar artefactos de grade.

**Clip, espuma e emissão:**
```hlsl
float surfaceWorldY = objectCenterY + (_FillLevel * objectRadius) + totalWave;
clip(surfaceWorldY - i.worldPos.y); // descarta fragmentos ACIMA da superfície

float belowSurface = surfaceWorldY - i.worldPos.y;
float foam = 1.0 - smoothstep(0.0, _FoamThreshold, belowSurface); // espuma na superfície

float glowPulse = sin(_Time.y * 2.0 + totalWave * 50.0) * 0.5 + 0.5;
fixed4 emissive = _GlowColor * glowPulse * _EmissionIntensity * foam;
```
`clip()` descarta fragmentos acima da linha de água (corte preciso da superfície). A espuma é a zona a poucos milímetros abaixo da superfície — mais brilhante e emissiva.

---

## 5. Stencil Portal + X-Ray

**Ficheiros:**
- `Assets/Shaders/StencilPortal.shader`
- `Assets/Shaders/StencilWorld.shader`

### O que fazem

Dois shaders que trabalham em conjunto para criar um **portal com efeito X-Ray**: o portal escreve uma máscara no stencil buffer; o segundo shader usa essa máscara para mostrar objetos "através" de paredes com um efeito de varrimento.

### `StencilPortal.shader` — Máscara do Portal

```hlsl
Stencil
{
    Ref 1; Comp Always; Pass Replace;
}
```
Escreve `1` em todos os fragmentos da geometria do portal — independentemente do que estiver à frente ou atrás. Toda a superfície do portal marca o stencil.

**Brilho de borda:**
```hlsl
float2 dist = abs(i.uv - 0.5) * 2.0; // [0,0] no centro, [1,1] nos cantos
float edge = max(dist.x, dist.y);     // distância à borda mais próxima
float glow = pow(edge, _Thickness);   // exponencial → concentra glow nas bordas

finalColor.a = glow;
```
O quad do portal fica quase invisível no centro e vai ganhando cor nas bordas — dando um frame iluminado ao portal.

---

### `StencilWorld.shader` — Objetos X-Ray

```hlsl
ZTest Always  // ignora o depth buffer — desenha mesmo atrás de paredes
Stencil
{
    Ref 1; Comp Equal; // só renderiza onde o stencil == 1 (dentro do portal)
}
```
Este shader renderiza apenas nos pixels onde o portal foi desenhado. Com `ZTest Always`, o objeto é visível mesmo que esteja geometricamente atrás de uma parede.

**Efeito de varrimento:**
```hlsl
float fresnel = 1.0 - saturate(dot(normalWorld, viewDirWorld));
fresnel = pow(fresnel, 2.0);

float scanline = sin(i.objPos.y * 50.0 + _Time.y * _ScanSpeed);
scanline = saturate(scanline * 0.5 + 0.5);

fixed4 finalColor = _ScanColor * fresnel;
finalColor.rgb += _ScanColor.rgb * scanline * 0.5;
```
Fresnel ilumina as bordas do objeto. Uma onda sinusoidal animada no eixo Y local cria linhas de scan que sobem pelo objeto — semelhante a um scanner.

---

## 6. Kerr — Black Hole Volumétrico

**Ficheiros:**
- `Assets/Shaders/ElPsyKongroo/Kerr.shader`
- `Assets/Scripts/SpinUp.cs` (`KerrAnomaly`)
- `Assets/Scripts/BlackHoleActivationButton.cs`

### O que faz

Simula um buraco negro com **raymarching volumétrico**. Dobra a luz usando gravidade simulada, renderiza um disco de acreção com doppler effect e usa `GrabPass` para distorcer o background visível através da lente gravitacional.

### Shader — Partes Importantes

**GrabPass — captura do background:**
```hlsl
GrabPass { "_BackgroundTexture" }
```
Antes de renderizar o buraco negro, a Unity faz uma cópia do ecrã. Esta textura é usada no final para simular a curvatura da luz em torno do buraco negro.

**Setup do ray (vertex shader):**
```hlsl
o.localPos = v.vertex.xyz;
o.localCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
```
Todo o raymarching é feito em **espaço local do objeto** — simplifica o cálculo gravitacional centrado na origem.

**Loop de raymarching:**
```hlsl
for (int step = 0; step < _MaxSteps; step++)
{
    float distFromCenter = length(rayPos);

    if (distFromCenter < _EventHorizon) { hitHorizon = true; break; }
    if (distFromCenter > 0.51) break; // sai da esfera do shader

    // Gravidade: curvatura do raio em direção ao centro
    float3 gravityPull = -normalize(rayPos) * (_Mass / (distFromCenter * distFromCenter + 0.001));
    rayDir = normalize(rayDir + gravityPull * _StepSize);
```
Em cada passo, o raio é desviado em direção ao centro com força proporcional a `Mass / r²` (lei gravitacional). O resultado é um raio que curva — simulando geodésicas em espaço-tempo curvo.

**Disco de acreção:**
```hlsl
float distToEquator = abs(rayPos.y);
if (distToEquator < _DiskThickness && distFromCenter > _EventHorizon && distFromCenter < _DiskRadius)
{
    float angle = atan2(rayPos.z, rayPos.x);
    float spin = angle + _Time.y * _SpinSpeed;

    float radialNoise = sin(distFromCenter * 40.0) * 0.5 + 0.5;
    float angularNoise = sin(spin * 6.0) * 0.5 + 0.5;

    // Doppler: mais brilhante do lado que se move em direção à câmara
    float3 spinDir = normalize(cross(float3(0, 1, 0), rayPos));
    float doppler = pow(max(0.0, dot(rayDir, spinDir)), 2.0);

    float stepGlow = verticalDensity * radialFalloff * ringNoise * _DiskDensity * _StepSize;
    accumulatedGlow.rgb += _DiskColor.rgb * stepGlow * (doppler * 3.0 + 0.2);
}
```
O disco existe apenas no plano equatorial (Y ≈ 0). O efeito Doppler é simulado com `dot(rayDir, spinDir)` — fragmentos do disco que "vêm na direção" da câmara ficam mais brilhantes.

**Lente gravitacional final:**
```hlsl
float2 distortion = (rayDir.xy - initialRayDir.xy) * _WarpStrength;
float2 lensedUV = originalScreenUV + distortion;
float4 background = tex2D(_BackgroundTexture, lensedUV);
return background + accumulatedGlow;
```
A diferença entre o raio inicial e o raio final (após curvar) é o deslocamento angular da luz — aplicado como offset de UV ao background capturado.

---

### Script — `KerrAnomaly` (`SpinUp.cs`)

Anima a "formação" do buraco negro — começa com massa e horizonte de evento zero e cresce até aos valores alvo.

```csharp
public void ActivateAnomaly()
{
    StartCoroutine(ChargeUpRoutine());
}

private IEnumerator ChargeUpRoutine()
{
    float duration = 3.0f;
    while (elapsed < duration)
    {
        float percent = elapsed / duration;
        blackHoleMat.SetFloat("_Mass",        Mathf.Lerp(0f, 0.08f,  percent * percent));
        blackHoleMat.SetFloat("_EventHorizon",Mathf.Lerp(0f, 0.02f,  percent * percent));
        blackHoleMat.SetFloat("_Spin",        Mathf.Lerp(0f, 5.94f,  percent));
        yield return null;
    }
}
```
A massa e o horizonte crescem quadraticamente (aceleração) enquanto o spin cresce linearmente — dá a sensação de carga progressiva.

---

### Script — `BlackHoleActivationButton.cs`

Botão VR (XR Interaction Toolkit) que ativa o buraco negro quando o jogador o pressiona.

```csharp
[RequireComponent(typeof(XRBaseInteractable))]
public class BlackHoleActivationButton : MonoBehaviour
{
    private void OnButtonSelected(SelectEnterEventArgs args)
    {
        if (hasTriggered) return;
        StartCoroutine(EnableAndPlayAnomaly());
    }

    private IEnumerator EnableAndPlayAnomaly()
    {
        hasTriggered = true;
        IsMicrowaveOn = true;
        anomalyRoot.SetActive(true);  // ativa o objeto do buraco negro
        anomalyScript.ActivateAnomaly(); // começa a animação de formação
        
        if (disableButtonAfterUse) buttonInteractable.enabled = false; // uso único
    }
}
```
`IsMicrowaveOn` é uma propriedade pública que o `MicrowaveReadingSteinerDriver` consulta para saber se pode ativar a viagem no tempo.

---

## 7. Reading Steiner

**Ficheiros:**
- `Assets/Shaders/ElPsyKongroo/ReadingSteiner.shader`
- `Assets/Scripts/ReadingSteiner.cs` (`ReadingSteinerEffect`)
- `Assets/Scripts/MicrowaveReadingSteinerDriver.cs`

### O que faz

Efeito de **distorção do campo visual** que simula a "leitura Steiner" de Steins;Gate — uma distorção realidade/glitch intensa que ocorre ao viajar entre worldlines. É um post-process que aplica tearing de linhas, aberração cromática forte e inversão de cores.

### Shader — Partes Importantes

**Tearing de linhas (horizontal tear):**
```hlsl
float tearBand = step(0.9, sin(uv.y * 50.0 + _Time.y * 20.0));
float tearOffset = tearBand * (_GlitchIntensity * 0.1);
uv.x += tearOffset;
```
`step(0.9, sin(...))` cria linhas horizontais binárias que piscam a 20Hz. As linhas "arrancadas" deslocam-se para a direita com intensidade proporcional a `_GlitchIntensity`.

**Aberração cromática de alta intensidade:**
```hlsl
float splitAmount = _GlitchIntensity * 0.08;
float r = tex2D(_MainTex, float2(uv.x + splitAmount, uv.y)).r;
float g = tex2D(_MainTex, uv).g;
float b = tex2D(_MainTex, float2(uv.x - splitAmount, uv.y)).b;
```
Separação dos canais muito mais agressiva que no CCTV Glitch — até 8% da largura do ecrã a `_GlitchIntensity = 1`.

**Inversão de cores:**
```hlsl
float invertTrigger = step(0.9, _GlitchIntensity);
finalColor = lerp(finalColor, 1.0 - finalColor, 
             invertTrigger * step(0.5, rand(uv * _Time.x)));
```
Acima de 90% de intensidade, pixels aleatórios invertem as suas cores — criando um efeito caótico de "falha na realidade".

---

### Script — `ReadingSteinerEffect.cs`

Post-process de câmara que conduz a `_GlitchIntensity` do shader ao longo de uma curva cúbica.

```csharp
private IEnumerator ShiftRoutine()
{
    float duration = 2.5f;
    while (elapsed < duration)
    {
        float percent = elapsed / duration;
        currentIntensity = Mathf.Lerp(0f, 1.2f, percent * percent * percent); // cúbico = aceleração
        yield return null;
    }
    currentIntensity = 1.2f;
    yield return new WaitForSeconds(0.4f); // pico mantém-se 0.4s
    currentIntensity = 0f;
}

void OnRenderImage(RenderTexture source, RenderTexture destination)
{
    steinerMaterial.SetFloat("_GlitchIntensity", currentIntensity);
    Graphics.Blit(source, destination, steinerMaterial);
}
```
A curva cúbica (`percent³`) faz a intensidade crescer lentamente no início e explodir no final — sensação dramática de "colapso da realidade".

---

### Script — `MicrowaveReadingSteinerDriver.cs`

**O script orquestrador principal.** Conecta todos os sistemas: micro-ondas VR → Reading Steiner → teleporte → CCTV → hologramas. É a sequência narrativa completa de viagem no tempo.

```csharp
[RequireComponent(typeof(XRGrabInteractable))]
public class MicrowaveReadingSteinerDriver : MonoBehaviour
```
O objeto (micro-ondas) pode ser agarrado em VR. Ao apertá-lo com o micro-ondas ligado (`microwaveSwitch.IsMicrowaveOn`), inicia a sequência.

**Sequência completa de worldline shift:**
```csharp
private IEnumerator WorldlineShiftSequence()
{
    // 1. Rampa de glitch (2.5s) — realidade a colapsar
    while (elapsed < rampDuration)
        readingSteinerEffect.currentIntensity = Mathf.Lerp(0f, maxIntensity, percent³);

    // 2. Pico + teleporte para a sala clone
    yield return new WaitForSeconds(0.4f);
    TeleportPlayer(cloneRoomTP);
    cctvEffect.enabled = true;  // câmeras de vigilância ligam-se na nova realidade

    readingSteinerEffect.currentIntensity = 0f; // realidade "estabiliza"

    // 3. Aguarda 15 segundos na nova worldline
    yield return new WaitForSeconds(15.0f);

    // 4. Segunda distorção + regresso
    readingSteinerEffect.currentIntensity = maxIntensity;
    yield return new WaitForSeconds(0.2f);
    TeleportPlayer(startRoomTP);
    cctvEffect.enabled = false;

    // 5. Altera o mundo: esconde/mostra objetos, injeta hologramas
    foreach (var obj in objectsOff) obj.SetActive(false);
    foreach (var obj in objectsOn)  obj.SetActive(true);

    // Injeta HologramController dinamicamente nos objetos da nova realidade
    foreach (Transform pai in paisParaHolograma)
    {
        HologramController hc = pai.GetComponent<HologramController>() 
                                ?? pai.gameObject.AddComponent<HologramController>();
        hc.materialHologramaBase = materialHolograma;
        hc.enabled = true;
    }
}
```
Este script usa **todos os outros sistemas** em sequência: Reading Steiner → teleporte → CCTV Glitch → HologramController. A injeção dinâmica de `HologramController` garante que os personagens da nova worldline aparecem como hologramas sem necessitar de setup prévio na cena.

---

## Resumo dos Sistemas

| Shader | Técnica Principal | Script Associado |
|---|---|---|
| CCTV Glitch | Post-process, aberração cromática, ruído | `CCTVGlitchEffect.cs` |
| Energy Shield | Geometry shader, Fresnel, ripple | `EnergyShieldController.cs` |
| Hologram | Rim light, scan lines, glitch geométrico | `HologramController.cs` |
| Radioactive Liquid | Multi-pass, Stencil, Value Noise, clip() | — |
| Starfield | Raymarching por camadas, hash procedural | — |
| Stencil Portal | Stencil write/read, ZTest Always | — |
| Kerr Black Hole | Raymarching volumétrico, GrabPass, doppler | `KerrAnomaly` + `BlackHoleActivationButton.cs` |
| Reading Steiner | Post-process, tearing, inversão de cor | `ReadingSteinerEffect.cs` + `MicrowaveReadingSteinerDriver.cs` |
