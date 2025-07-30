# SubEmailSender

![.NET Version](https://img.shields.io/badge/.NET-8.0-blueviolet)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-supported-orange)
![Serilog](https://img.shields.io/badge/Serilog-logging-green)
![MailKit](https://img.shields.io/badge/MailKit-SMTP-blue)
![License](https://img.shields.io/badge/license-MIT-brightgreen)
![Build](https://img.shields.io/badge/build-passing-brightgreen)

---

## 📧 Sobre o Projeto

**SubEmailSender** é um app console .NET que funciona como subscriber de RabbitMQ, recebendo mensagens de uma fila e enviando e-mails via SMTP. Utiliza:

- RabbitMQ.Client
- MailKit / MimeKit
- Serilog
- Newtonsoft.Json

---

## 🚀 Como Funciona

- Conecta ao RabbitMQ e assina uma fila específica.
- Recebe mensagens JSON com dados do e-mail.
- Envia os e-mails via SMTP usando MailKit.
- Loga todas as etapas do processo com Serilog.

---

## ⚙️ Tecnologias e Bibliotecas

- [.NET 8.0 ou superior](https://dotnet.microsoft.com/)
- RabbitMQ.Client
- MailKit / MimeKit
- Serilog
- Newtonsoft.Json

---

## 📥 Instalação

1. **Pré-requisitos**
    - .NET 8.0 ou superior
    - Instância do RabbitMQ disponível

2. **Clone o repositório**
```bash
  git clone https://github.com/seu-usuario/SubEmailSender.git
  cd SubEmailSender
```


3. **Restaure as dependências**
```bash
  dotnet restore
```


---

## 🔧 Configuração

### appsettings.json

Para que o app funcione, crie um arquivo `appsettings.json` na raiz do projeto com seu ambiente SMTP. Utilize o exemplo abaixo como base:

#### appsettings.example.json
```json
{
    "Settings": {
        "Host": "",
        "Port": 0,
        "User": "",
        "Password": "",
        "FromEmail": "",
        "FromName": ""
    }
}
```

- **Host**: Endereço do servidor SMTP (exemplo: sandbox.smtp.mailtrap.io)
- **Port**: Porta SMTP (exemplo: 465)
- **User**: Usuário SMTP
- **Password**: Senha SMTP
- **FromEmail**: E-mail do remetente
- **FromName**: Nome do remetente

Renomeie `appsettings.example.json` para `appsettings.json` e preencha com seus dados.

---

## 📨 Payload de Exemplo

Envie na fila RabbitMQ um JSON como este:

```json
{
    "to": "destinatario@exemplo.com",
    "subject": "Seu recibo Foodie",
    "body": "<h1>Obrigado por comprar conosco!</h1>"
}
```


---

## ⏯️ Como Rodar

Execute o projeto via terminal:
```text
    dotnet run --project SubEmailSender
```

O console exibirá logs detalhados do processamento das mensagens.

---

## 📝 Licença

Distribuído sob a licença [MIT](LICENSE).

---

**Contribuições são bem-vindas!**  
Abra issues e pull requests para sugestões, críticas ou melhorias.
