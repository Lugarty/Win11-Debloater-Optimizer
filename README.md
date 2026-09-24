# Win11 Debloater & Optimizer

![C#](https://img.shields.io/badge/C%23-12.0-512BD4?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Windows](https://img.shields.io/badge/Windows-11-0078D6?logo=windows)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-5391FE?logo=powershell)

Aplicação desktop em **C#/.NET 8 + WinForms** para automatizar manutenção, debloat e ajustes do Windows 11 por meio de **PowerShell**, Registry, serviços, tarefas agendadas, AppX, DISM, SFC e `powercfg`.

O projeto centraliza operações de sistema em uma interface única, com execução assíncrona, logs em tempo real, confirmações para etapas destrutivas e tentativa de criação de ponto de restauração antes das alterações.

## O que este projeto demonstra

- Integração entre **C# e PowerShell** usando `ProcessStartInfo`.
- Execução assíncrona para reduzir bloqueios da interface em tarefas demoradas.
- Captura separada de **stdout** e **stderr**.
- Atualização thread-safe da interface a partir de processos externos.
- Manipulação de **Registry** em `HKCU` e `HKLM`.
- Controle de serviços e tarefas agendadas.
- Remoção de pacotes AppX instalados e provisionados.
- Uso de ferramentas nativas como **DISM**, **SFC**, `powercfg` e `cleanmgr`.
- Elevação UAC via manifesto.
- Confirmação adicional para operações de maior impacto.
- Logs categorizados em tempo real.

## Fluxo técnico

```text
WinForms UI
    |
    v
OtimizacaoTask
    |
    v
Ação C#
    |
    v
RunPS()
    |
    +--> script PowerShell temporário
    +--> powershell.exe
    +--> stdout --------------+
    +--> stderr --------------+--> TrataLinha() --> log da interface
```

Cada card da interface representa uma `OtimizacaoTask`, contendo nome, descrição, tempo estimado, ação assíncrona e, quando necessário, uma confirmação adicional.

## Modos de execução

### Pacote completo

Inclui:

- ponto de restauração;
- privacidade e telemetria;
- remoção seletiva de apps;
- OneDrive, Copilot, Recall e Widgets;
- serviços e tarefas agendadas;
- desempenho e Game Mode;
- limpeza de temporários;
- limpeza do repositório de componentes;
- `DISM /RestoreHealth`;
- `sfc /scannow`.

### Modo rápido

Executa as etapas mais curtas e **não executa** a limpeza profunda de componentes, DISM e SFC.

## Segurança e reversibilidade

O fluxo completo tenta:

1. habilitar a Restauração do Sistema na unidade `C:\`;
2. criar um ponto chamado `Win11Debloater`.

Operações de maior impacto pedem confirmação adicional. A etapa com `DISM /ResetBase`, por exemplo, alerta que pode impedir a desinstalação de atualizações antigas.

> O ponto de restauração depende da configuração do próprio Windows e não substitui um backup dos dados.

## Logs e diagnóstico

O executor captura a saída padrão e os erros do PowerShell e envia as linhas para a interface em tempo real.

Categorias usadas:

- `[OK]` — conclusão reportada pelo script;
- `[AVISO]` — condição que exige atenção;
- `[REG]` — Registry;
- `[SVC]` — serviços;
- `[TASK]` — tarefas agendadas;
- `[APP]` — AppX;
- `[FILE]` — arquivos/limpeza;
- `[DISM]` / `[SFC]` — manutenção da imagem e arquivos do sistema.

### Semântica do status

O status visual **“Concluído”** indica que o fluxo da tarefa terminou sem uma exceção C# não tratada. Alguns comandos do Windows podem retornar avisos ou falhas parciais sem interromper todo o pacote; por isso, o log e o estado final do sistema devem ser validados em operações críticas.

## Principais operações

### Privacidade e telemetria
- políticas de coleta de dados;
- Advertising ID;
- histórico de atividades;
- sugestões e consumer features;
- serviços como `DiagTrack`, `dmwappushservice` e `WerSvc`.

### Apps e integração do sistema
- remoção seletiva de AppX;
- remoção de pacotes provisionados;
- OneDrive;
- Copilot, Recall e Widgets;
- ajustes da busca e interface.

### Manutenção
- limpeza de temporários;
- cache do Windows Update;
- Delivery Optimization;
- Lixeira;
- `DISM /StartComponentCleanup`;
- `DISM /ResetBase`;
- `DISM /RestoreHealth`;
- `sfc /scannow`.

## Tecnologias

| Área | Tecnologia |
|---|---|
| Linguagem | C# |
| Runtime | .NET 8 |
| UI | WinForms |
| Automação | Windows PowerShell |
| Sistema | Registry, Services, Scheduled Tasks, AppX |
| Manutenção | DISM, SFC, cleanmgr, powercfg |

## Como executar

### Pré-requisitos

- Windows 11;
- privilégios de administrador;
- .NET 8 SDK apenas se for compilar o projeto.

### Pelo código-fonte

```bash
git clone https://github.com/Lugarty/Win11-Debloater-Optimizer.git
cd Win11-Debloater-Optimizer
dotnet run
```

### Publicar executável

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## Limitações conhecidas

- Algumas políticas variam entre edições e builds do Windows 11.
- Vários cmdlets usam tratamento tolerante a erro para que uma etapa ausente em determinada máquina não interrompa o pacote inteiro.
- O log deve ser consultado para identificar avisos e falhas parciais.
- Alterações de Registry, serviços e componentes do Windows devem ser validadas antes de uso em máquinas de produção.

## Roadmap

- separar executor, regras de otimização e UI em camadas distintas;
- diferenciar visualmente **sucesso**, **concluído com avisos** e **falha**;
- validar códigos de saída de comandos externos de forma mais granular;
- adicionar testes automatizados para componentes isoláveis do Windows;
- ampliar documentação por versão/build do Windows.

## Aviso

Este utilitário altera configurações sensíveis do Windows, incluindo Registry, serviços, tarefas agendadas, aplicativos e componentes do sistema.

Use somente se você entender as mudanças realizadas. Faça backup dos seus dados antes de executar operações destrutivas.

## Licença

Projeto disponibilizado sob licença MIT. Consulte [LICENSE](LICENSE).

## Autor

**Anísio Oliveira Albuquerque Filho**  
GitHub: [@Lugarty](https://github.com/Lugarty)
