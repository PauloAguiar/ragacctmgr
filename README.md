# RO Contas

**RO Contas** é um gerenciador de contas para Ragnarok Online, feito em WPF (.NET), com suporte para PIN, OTP, Kafra, Senha.

## Como compilar

1. **Pré-requisitos:**  
   - [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) ou superior
   - Windows 10/11

2. **Compilar e rodar em modo desenvolvimento:**
   ```sh
   dotnet build
   dotnet run
   ```

3. **Gerar executável único para distribuição:**
   - Execute o script:
     ```sh
     publish-singlefile-release.bat
     ```
   - O `.exe` estará em `bin\Release\net9.0-windows\win-x64\publish\`