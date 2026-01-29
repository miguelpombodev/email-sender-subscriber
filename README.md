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
  "to": "recipient@example.com",
  "subject": "Subject text",
  "body": "Email content here"
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
