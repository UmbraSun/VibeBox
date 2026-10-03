# VibeBox

VibeBox is a scalable media and communication platform focused on video calls, video and image sharing, and reliable media storage.

The project is designed with a strong focus on:

* scalability and horizontal scaling;
* efficient media storage;
* high-throughput file delivery;
* low-latency realtime communication;
* asynchronous media processing;
* efficient use of infrastructure resources;
* observability and production-oriented architecture.

## Core Features

The platform is intended to provide:

* user authentication and authorization;
* one-to-one and group video calls;
* video and image uploads;
* media storage and sharing;
* media previews and thumbnails;
* media processing and transcoding;
* realtime communication;
* access control and sharing permissions;
* media metadata management.

## Technology Stack

### Backend

* .NET / ASP.NET Core
* C#
* PostgreSQL
* Redis
* Apache Kafka
* WebRTC
* SignalR / WebSockets
* OpenTelemetry

### Frontend

* React
* TypeScript

### Media Infrastructure

* S3-compatible Object Storage
* CDN
* FFmpeg
* WebRTC
* SFU for scalable group video communication

### Infrastructure

* Docker
* Docker Compose
* Nginx / Load Balancer
* Prometheus
* Grafana

## Architecture Principles

VibeBox follows a media-first architecture.

Large media files should not be transferred through the application API. The API is responsible for authentication, authorization, metadata, upload orchestration, and business logic, while the actual media transfer is performed directly between the client and object storage whenever possible.

```text
                         ┌──────────────────┐
                         │      React       │
                         └────────┬─────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │                           │
                    ▼                           ▼
             ASP.NET Core                  Object Storage
                    │                           │
          ┌─────────┼─────────┐                │
          │         │         │                │
          ▼         ▼         ▼                ▼
      PostgreSQL  Redis     Kafka             CDN
                              │
                              ▼
                       Media Workers
                              │
                              ▼
                           FFmpeg
```

### Media Storage

Object Storage is used for binary media data.

PostgreSQL stores metadata and references to stored objects rather than the media itself.

```text
PostgreSQL
    │
    └── MediaFile
          ├── Id
          ├── OwnerId
          ├── StorageKey
          ├── ContentType
          ├── Size
          ├── Checksum
          └── Status

Object Storage
    │
    └── users/
         └── {userId}/
              ├── images/
              ├── videos/
              ├── thumbnails/
              └── previews/
```

This separation allows the API and database to scale independently from media storage and delivery.

### Video Calls

WebRTC is used for realtime audio and video communication.

For scalable group calls, the architecture is designed to support an SFU (Selective Forwarding Unit) instead of relying on a full mesh connection between participants.

### Asynchronous Processing

Long-running and resource-intensive operations should be performed asynchronously.

Examples include:

* video transcoding;
* thumbnail generation;
* preview generation;
* media metadata extraction;
* media validation;
* cleanup of unused objects.

Kafka is used as the event backbone between the application and background workers.

## Data Storage

### PostgreSQL

PostgreSQL is the primary relational database and source of truth for application state.

It stores:

* users;
* rooms;
* calls;
* participants;
* media metadata;
* permissions;
* sharing information;
* other transactional application data.

### Redis

Redis is used for data that requires low-latency access, such as:

* caching;
* distributed locks;
* short-lived state;
* rate limiting;
* realtime coordination where appropriate.

### Object Storage

Object Storage is responsible for large binary objects:

* images;
* original videos;
* transcoded videos;
* thumbnails;
* previews.

The application should use presigned URLs and direct client-to-storage transfers where appropriate.

## Scalability

The system is designed to support horizontal scaling.

Application instances should remain as stateless as practical so that additional instances can be added behind a load balancer.

```text
                    Load Balancer
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
           API #1      API #2      API #3
              │          │          │
              └──────────┼──────────┘
                         │
                ┌────────┴────────┐
                ▼                 ▼
            PostgreSQL          Redis
                │
              Kafka
                │
       ┌────────┼────────┐
       ▼        ▼        ▼
    Worker    Worker    Worker
```

Media delivery is separated from application traffic through Object Storage and CDN.

## Storage Efficiency

Storage usage is an important design consideration.

The platform should consider:

* media deduplication where appropriate;
* checksums;
* efficient image formats;
* video transcoding;
* multiple media resolutions;
* thumbnails and previews;
* lifecycle policies;
* cleanup of orphaned objects;
* CDN caching;
* resumable and multipart uploads.

Derived media should only be retained when there is a clear product or technical reason to keep it.

## Observability

The platform is intended to provide production-grade observability using:

* OpenTelemetry;
* structured logging;
* metrics;
* distributed tracing;
* health checks;
* Prometheus;
* Grafana.

Important metrics will include:

* API latency;
* request throughput;
* error rates;
* upload/download throughput;
* media processing time;
* worker queue latency;
* storage usage;
* cache hit rate;
* WebRTC connection statistics.

## Project Structure

The project will use a modular structure with a clear separation between application logic, infrastructure, media processing, and realtime communication.

```text
VibeBox/
│
├── src/
│   ├── VibeBox.Api/
│   ├── VibeBox.Application/
│   ├── VibeBox.Domain/
│   ├── VibeBox.Infrastructure/
│   │
│   ├── VibeBox.Media/
│   ├── VibeBox.Media.Worker/
│   ├── VibeBox.Realtime/
│   ├── VibeBox.Realtime.Worker/
│   │
│   ├── VibeBox.Contracts/
│   └── VibeBox.BuildingBlocks/
│
├── tests/
│   ├── VibeBox.UnitTests/
│   ├── VibeBox.IntegrationTests/
│   └── VibeBox.ArchitectureTests/
│
├── frontend/
│   └── VibeBox.Web/
│
├── infrastructure/
│   ├── docker/
│   ├── postgres/
│   ├── kafka/
│   ├── redis/
│   └── storage/
│
├── README.md
├── .gitignore
└── docker-compose.yml
```

The physical project structure may evolve as the system grows. New infrastructure or services should be introduced only when they solve a concrete scalability, reliability, or architectural requirement.

## Development Philosophy

VibeBox is intended to be developed as a production-oriented project rather than as a minimal proof of concept.

The main priorities are:

1. Correctness
2. Scalability
3. Efficient resource usage
4. Reliability
5. Performance
6. Maintainability
7. Observability

Complexity should be introduced only when it provides a measurable or architectural benefit.
