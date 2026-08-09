<h1 align="center">Email Sender Subscriber</h1>

<p align="center">
<img style="width: 17%" src="https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff" alt=".NET 8">
<img style="width: 25%" src="https://img.shields.io/badge/-rabbitmq-%23FF6600?style=flat&logo=rabbitmq&logoColor=white" alt="RabbitMQ">
</p>

## Overview

`email-sender-subscriber` is a .NET background worker that subscribes to a RabbitMQ queue and sends email messages via SMTP. It is designed to integrate with other applications — for example, a NestJS API that pushes email jobs to a queue — enabling decoupled, scalable email delivery.

This project supports running both locally with `dotnet run` and in containerized environments with `docker-compose up`. However, the priority use case is deployment via Docker in a multi-service architecture.

## Features

- Consumes email jobs from RabbitMQ
- Sends emails through a configurable SMTP server
- Environment variable and configuration file support
- Runs as a background service or Docker container

## Prerequisites

Before running this worker, you must have:

- A running **RabbitMQ** instance
- SMTP credentials (Host, Port, Username, Password, From email/name)

## Configuration

Configuration is provided via environment variables or `appsettings.json`. The expected variables use the `.NET Options` pattern and map to the `Smtp` section:
```
Smtp__Host
Smtp__Port
Smtp__User
Smtp__Password
Smtp__FromEmail
Smtp__FromName
```


Example (bash):

```bash
export Smtp__Host="smtp.yourprovider.com"
export Smtp__Port="587"
export Smtp__User="username"
export Smtp__Password="password"
export Smtp__FromEmail="noreply@domain.com"
export Smtp__FromName="Your App"
```

For RabbitMQ, ensure your queue configuration matches the settings defined in `appsettings.json` or via environment variables.

## Running the Worker

### Local (development)

Use the .NET CLI:

```bash
dotnet run --project SubEmailSender/SubEmailSender.csproj
```
Make sure all required environment variables are set before running the application.

### Docker (recommended)

The included docker-compose.yml can start the worker along with RabbitMQ:
```bash
docker-compose up
```

This will:

- Start a RabbitMQ instance
- Build and run the email sender worker
- Adjust SMTP credentials and RabbitMQ settings using environment variables or a `.env` file

## Usage with Another Application

In a typical scenario, your API (for example, a NestJS application) will:

1. Publish email messages to a RabbitMQ queue
2. The worker will consume those messages
3. Emails will be sent via the configured SMTP provider

### Example queue payload (JSON)
```json
{
  "messageId": "0198f8c4-7c9a-7b21-9e3c-123456789abc",
  "conversationId": "0198f8c4-7c9a-7b21-9e3c-123456789abd",
  "sourceAddress": "rabbitmq://localhost/test-producer",
  "destinationAddress": "rabbitmq://localhost/sub-email-sender",
  "messageType": [
    "urn:message:SubEmailSender.Models:EmailToBeSend"
  ],
  "message": {
    "to": "teste@example.com",
    "subject": "Teste MassTransit",
    "body": "<html><body><h1>Teste MassTransit</h1><p>Mensagem de teste.</p></body></html>"
  },
  "sentTime": "2026-08-09T20:00:00Z"
}
```

### Example queue payload (JSON) with more properties
```json
{
  "messageId": "0198f8c4-7c9a-7b21-9e3c-123456789abc",
  "conversationId": "0198f8c4-7c9a-7b21-9e3c-123456789abd",
  "sourceAddress": "rabbitmq://localhost/test-producer",
  "destinationAddress": "rabbitmq://localhost/sub-email-sender",
  "messageType": [
    "urn:message:SubEmailSender.Models:EmailToBeSend"
  ],
  "message": {
    "to": "destinatario@example.com",
    "cc": [
      "copia1@example.com",
      "copia2@example.com"
    ],
    "bcc": [
      "copia-oculta1@example.com",
      "auditoria@example.com"
    ],
    "subject": "Teste completo do Email Sender",
    "body": "<!DOCTYPE html><html><body style=\"margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,sans-serif;\"><div style=\"max-width:600px;margin:40px auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);\"><div style=\"background:#1f2937;padding:30px;text-align:center;color:#ffffff;\"><h1 style=\"margin:0;font-size:28px;\">Teste de E-mail</h1><p style=\"margin:10px 0 0;font-size:14px;color:#d1d5db;\">MassTransit + RabbitMQ + SMTP</p></div><div style=\"padding:35px;\"><h2 style=\"color:#111827;margin-top:0;\">Olá!</h2><p style=\"color:#4b5563;line-height:1.7;\">Este é um teste completo do serviço de envio de e-mails.</p><div style=\"background:#f3f4f6;border-left:4px solid #2563eb;padding:16px;margin:25px 0;\"><strong style=\"color:#111827;\">Status:</strong><span style=\"color:#16a34a;margin-left:8px;\">Mensagem processada com sucesso</span></div><table style=\"width:100%;border-collapse:collapse;margin:25px 0;\"><tr><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#6b7280;\">To</td><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#111827;\">destinatario@example.com</td></tr><tr><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#6b7280;\">CC</td><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#111827;\">copia1@example.com, copia2@example.com</td></tr><tr><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#6b7280;\">Attachments</td><td style=\"padding:10px;border-bottom:1px solid #e5e7eb;color:#111827;\">2 arquivos</td></tr></table><p style=\"color:#4b5563;line-height:1.7;\">Também estamos testando CC, BCC e anexos.</p><div style=\"text-align:center;margin-top:30px;\"><a href=\"https://example.com\" style=\"display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:6px;\">Abrir aplicação</a></div></div><div style=\"background:#f9fafb;padding:20px;text-align:center;color:#9ca3af;font-size:12px;\">Este é um e-mail automático de teste.</div></div></body></html>",
    "isBodyHtml": true,
    "attachments": [
      {
        "fileName": "teste.txt",
        "contentType": "text/plain",
        "contentBase64": "SGVsbG8gZnJvbSBNYXNzVHJhbnNpdCEK"
      },
      {
        "fileName": "dados.json",
        "contentType": "application/json",
        "contentBase64": "eyJzdGF0dXMiOiAib2siLCAic291cmNlIjogImVtYWlsLXNlbmRlciJ9"
      }
    ]
  },
  "sentTime": "2026-08-09T20:00:00Z"
}
```
Your application is only responsible for publishing messages to the queue. The worker handles delivery and SMTP communication.

### Deployment

This service is suitable for containerized environments such as:

- Kubernetes
- Amazon ECS
- Docker Swarm

Ensure all secrets and environment variables are managed securely by your orchestration platform.

### Contribution

Contributions are welcome. Feel free to open issues or submit pull requests.

### License

This project is licensed under the MIT License. See the LICENSE
file for details.
