# Steven Universe: Unleash the Light ? Android Port Kit & BYOG Launcher

Port nativo nao oficial de **Steven Universe: Unleash the Light** (PC/Steam) para **Android (`ARM64` e `ARMv7`)**, projetado para rodar com paridade visual com o PC tanto em smartphones modernos (60 FPS) quanto em dispositivos de entrada com **2 GB de RAM e GPU ARM Mali (30 ou menos)**.

> **Aviso Legal:** Este reposit?rio **n?o cont?m** assets, texturas, ?udios, AssetBundles ou c?digo propriet?rio do jogo original. Para utilizar este port ou o Launcher BYOG, ? necess?rio possuir uma c?pia legal de *Steven Universe: Unleash the Light* na Steam.

---

## Principais Recursos

- **Launcher Android BYOG (*Bring Your Own Game*) integrado:** permite instalar um APK leve e importar o `.zip` dos bundles Android (`utl_bundles`) direto da pasta **Downloads** usando o seletor nativo do Android (Android 6 ao 15, sem precisar de permiss?es extras).
- **Shaders GLES 2.0/3.0 customizados:** reconstru??o dos materiais de cen?rios e habilidades (Quarto da Rose, Lapis Lazuli, Bismuto, Pal?cio da Luz, George, Projetores e efeitos de batalha).
- **Submenu de Conquistas (estilo Steam):** todas as 34 conquistas oficiais com ?cones, tradu??o em PT-BR, barra de progresso, filtros (*Todas / Desbloqueadas / Bloqueadas*) e sincroniza??o autom?tica com os Save Slots locais.
- **Ajustes pensados para telas m?veis:**
  - Seletor de **Resolu??o in-game** adaptado ? propor??o da tela Android.
  - Op??o **Caixas Charm (`NORMAL / GRANDE / EXTRA`)** nas Configura??es para melhorar a leitura em qualquer tela.
  - Perfis de desempenho (**Auto / 60 FPS / 30 FPS para 2 GB RAM**).
- **Launcher Builder para PC (`UTL_Android_Launcher.pyw`):** interface gr?fica no Windows para aplicar os scripts/shaders do port, limpar cache do IL2CPP, compilar o APK em segundo plano via Unity `2020.3.12f1` e instalar via ADB.

---

## Como Jogar (APK Launcher BYOG no Android)

1. Baixe e instale o **`UTL_Android_Launcher_BYOG.apk`** na aba **Releases**.
2. Coloque o arquivo `.zip` com os bundles convertidos para Android (`utl_bundles`) na pasta **Download** do celular.
3. Abra o app, toque em **`SELECIONAR ZIP (ANDROID)`** e escolha o arquivo.
4. Aguarde a extra??o finalizar, ajuste o perfil/tamanho das caixas se desejar e toque em **`JOGAR`**.

---

## Como Compilar no PC (Port Kit)

### Requisitos
- C?pia legal de *Steven Universe: Unleash the Light* (Steam) exportada com **AssetRipper**.
- **Unity 2020.3.12f1** com m?dulo **Android Build Support (IL2CPP + SDK/NDK)**.
- **Python 3.8+** no Windows.
- *(Opcional)* **ADB** configurado no PATH.

### Passo a passo
1. D? duplo clique em **`Iniciar_Launcher.bat`** (ou execute `python UTL_Android_Launcher.pyw`).
2. Indique o caminho do `Unity.exe` (`2020.3.12f1`) e da pasta `ExportedProject`.
3. Escolha a arquitetura (**ARM64**, **ARMv7** ou **Universal**) e o perfil (**Release** ou **Master**).
4. Clique em **`FAZER TUDO (1 + 2 + 3)`** ? ou use no Unity o menu **`UTL Port > Compilar APK Launcher BYOG (Leve, sem AssetBundles)`**.

---

## Cr?ditos e Colabora??o

- **Idealiza??o, testes, engenharia de port e builds Android:** [TreinerIsaac](https://github.com/TreinerIsaac)
- **Colabora??o t?cnica (scripts C#, shaders GLES, otimiza??es de desempenho e Launcher BYOG):** **Gemini**

---

*English summary: Unofficial native Android port kit & BYOG Launcher for Steven Universe: Unleash the Light (Unity 2020.3.12f1, ARM64 & ARMv7/2GB Mali), featuring custom GLES shaders, 60 FPS optimizations, Steam-style Achievements menu, configurable Charm box scaling, and an embedded Android Storage Access Framework `.zip` importer. No copyrighted game assets are included.*
