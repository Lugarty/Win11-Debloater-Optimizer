# ⚡ Win11 Debloater & Optimizer

![C#](https://img.shields.io/badge/C%23-12.0-purple?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?logo=dotnet)
![Windows](https://img.shields.io/badge/Windows-11-0078D6?logo=windows)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-blue?logo=powershell)

> 🛡️ Um utilitário desktop open-source para otimização, debloat e manutenção profunda do Windows 11, desenvolvido em C# com execução assíncrona de scripts PowerShell, interface minimalista e logs detalhados em tempo real.

## ✨ Diferenciais

- ⚡ **Execução assíncrona não-bloqueante** – A interface nunca trava, mesmo durante o DISM/SFC que podem levar até 30 minutos.
- 🎨 **Interface minimalista estilo Windows 11** – Sidebar com cards informativos + console de log com cores semânticas.
- 📊 **Logs detalhados em tempo real** – Cada ação mostra exatamente o que está sendo feito (registros alterados, serviços parados, apps removidos).
- 🔐 **Confirmações inteligentes** – Tarefas perigosas (remoção de apps, limpeza profunda de updates) pedem confirmação antes de executar.
- 🚀 **Dois modos de execução** – Pacote completo (com DISM/SFC e limpeza profunda) ou modo rápido (sem operações demoradas).
- 🛡️ **Elevação UAC automática** – Solicita permissões de administrador automaticamente ao iniciar, garantindo acesso total ao sistema.
- 🔄 **Ponto de restauração automático** – Cria backup seguro do registro e sistema antes de aplicar alterações.
- 🧹 **Limpeza profunda de updates** – Remove componentes substituídos do Windows Update usando `DISM /StartComponentCleanup` e `/ResetBase`.

## 🎯 Público-Alvo

- 💻 **Usuários do Windows 11** que querem um sistema mais limpo, rápido, privado e sem anúncios nativos.
- 🎮 **Gamers** que buscam otimizações de desempenho, ativação do Game Mode e remoção de bloatware em segundo plano.
- 🔒 **Entusiastas de privacidade** que desejam desativar telemetria, rastreamento, Recall, Copilot e coleta de dados.
- 🛠️ **Power users e técnicos** que precisam de uma ferramenta completa, portátil e de um clique para manutenção do sistema.
- 👨‍💻 **Desenvolvedores** interessados em aprender sobre execução de PowerShell em C#, manipulação de registro e interfaces WinForms.

## 🚀 Como Usar

### Opção 1: Executar o EXE
1. Baixe o arquivo `Win11Debloater.exe` da pasta principal do repositório ou da seção [Releases](../../releases).
2. Execute o arquivo (o Windows pedirá permissão de administrador automaticamente via UAC).
3. Escolha uma otimização individual nos cards ou clique em **"⚡ Executar tudo"** / **"🚀 Sem DISM/SFC"**.

### Opção 2: Compilar do código-fonte
1. Clone o repositório
"git clone https://github.com/Lugarty/Win11-Debloater-Optimizer" e depois
"cd Win11Debloater"

2. Execute em modo desenvolvimento
"dotnet run"

3. Ou compile o executável único com o comando "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ".

## 🛠️ Otimizações Incluídas

- 🔒 Privacidade & Telemetria
- ✅ Desativa coleta de dados e serviços de rastreio (DiagTrack, dmwappushservice, WerSvc).
- ✅ Remove ID de publicidade, histórico de atividades e sugestões de consumer features.
- 🗑️ Debloat & Apps
- ✅ Remove apps nativos inúteis (Bing News, Weather, Solitaire, Phone Link, Clipchamp, etc).
- ✅ Desinstala OneDrive completamente e remove da inicialização.
- ✅ Remove Widgets, Copilot e desativa análise de IA (Recall).
- ⚙️ Interface & Sistema
- ✅ Menu de contexto clássico (estilo Windows 10).
- ✅ Busca do Menu Iniciar restrita a arquivos locais (sem sugestões do Bing).
- ✅ Desativação de tarefas agendadas de telemetria e CEIP.
- 🚀 Desempenho & Manutenção
- ✅ Plano de energia Alto Desempenho, Game Mode e desativação de GameDVR.
- ✅ Limpeza profunda de temporários, cache, Prefetch e Lixeira.
- ✅ Limpeza do repositório de componentes do Windows Update (DISM /ResetBase).
- ✅ Verificação e reparo de integridade do sistema (DISM /RestoreHealth + sfc /scannow).

## 📊 Logs Inteligentes

O sistema filtra automaticamente poluição visual (barras de progresso do DISM) e colore mensagens por categoria:

- 🟢 **[OK]** / Sucessos
- 🟡 **[AVISO]** / Permissões ignoradas
- 🔴 **[ERRO]** / Falhas de execução
- 🔵 **[REG]**, **[SVC]**, **[APP]**, **[TASK]** / Ações específicas

Tarefas destrutivas (remoção de apps, limpeza profunda) exigem confirmação do usuário antes de executar.

## ⚠️ Aviso Importante
### Este utilitário faz alterações profundas no sistema Windows:
- Modifica chaves de registro (HKLM e HKCU).
- Remove aplicativos provisionados na imagem do sistema.
- Desativa serviços nativos da Microsoft.
- Executa comandos com privilégios elevados (TrustedInstaller / SYSTEM).
- O aplicativo cria um Ponto de Restauração automaticamente na primeira etapa, mas é altamente recomendado que você faça seu próprio backup antes de usar. Use por sua conta e risco. O código é 100% open-source para que você possa auditar exatamente o que está sendo feito no seu PC.

## 📄 Licença

Este projeto é open-source e está disponível sob a licença **MIT**. Sinta-se livre para usar, modificar, forkar e distribuir. Consulte o arquivo [LICENSE](LICENSE) para obter mais detalhes.

## 👥 Desenvolvedor

| Nome | Contato |
|------|---------|
| **Anisio Oliveira Albuquerque Filho** | anisioalbuquerque71@gmail.com |