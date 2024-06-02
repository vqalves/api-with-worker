## What is this project
Experimentation project that tests:
- how to use API with workers within the same process
- persistent queue that keeps data even if RabbitMQ instance goes down
- run RabbitMQ and API within the same docker-compose with healthcheck to ensure the correct availability sequence

## Setup
Change the volume path inside rabbitmq in docker-compose.yaml to a folder inside your local computer. Docker-compose is in the root folder.

## How to run
Easiest way is to run `docker compose up` on the root folder. After it runs:
- Local API: <a href="http://localhost:8080/swagger">http://localhost:8080/swagger</a>
- Local Rabbit: <a href="http://localhost:15672">http://localhost:15672</a> (user: guest / pass: guest).

Output is through logs only. 

Workers will NOT print a log per message, only after a batch of 1k messages is received.