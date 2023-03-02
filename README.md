# Open Telemetry Example

Its a list of sample applications involving interactions between each other to mimic the behaviour of
a micro services. This application uses 2 types of communication for interaction, one is rest based
http protocol and other one is asynchronous communication using kafka.

1. It uses open telemetry API and its Dot.net SDK.
2. It uses available open telemetry instrumentation library for asp.et core, http calls and SQL etc.
3. It uses custom tracing for Kafka.
4. Uses jaeger exporter for collection of all the traces.
5. It uses Prometheus for metrics collection and then integrated with Grafana.
6. It uses grafana as the backend for setting up of dashboards.
7. Uses Prometheus Alerts manager for sending alerts.

## How to set up

1. Run docker compose up from root of the downloaded application
2. Then run each application locally.
3. Go to http://localhost:16686/ to check tracing.
4. Go to http://localhost:9090/ to check for metrics in prometheus.
5. Go to http://localhost:3000/ for grafana.

## High level interaction diagram

![](Images/Blank%20diagram.jpeg)

#### Here is the explanation of the flow.

1. Executor is the worker service which makes 2 http post call to /PostMessage and /PostMessage/Post-Message in App1(Api) for every 5000MS
2. /PostMessage of App1 API makes call to another API App3(/PostMessage) and it will add the data to SQL DB.
3. /PostMessage/Post-Message of APP1 API will send a message to Kafka topic "Purchase" and returns a response.
4. App2 which is a console application which will consume message from Kafka topic "Purchase" and sends it to App3 API(/PostMessage/Post-Message).
5. /PostMessage/Post-Message of App3 API will send message to Kafka topic "Purchase2" and returns response.
6. App4 worker service will consume data from Kafka topic "Purchase2" and saves it in Redis cache.

#### Sample Grafana Tempo tracing screen shot

![](Images/Grafana%20Node%20graph.png)

![](Images/Tempo%20Tracing.png)

#### What insight you are getting from this tracing image?

I have used Open telemetry for tracing.

1. How much time it took to complete entire flow?
2. How much time Db calls are taking?
3. How much time kafka message posting is taking?
4. How much time Redis operations are taking?
5. With these information, we can identify any bottleneck and performance related issues.

#### sample metrics dashboard from grafana and prometheus

I have used prometheus for scraping metrics and set up Prometheus as data source in Grafana so that
we could see metrics in Grafana dashboard itself.

![](Images/Grafan%20Metrics%20dashboard.png)

![](Images/Grafana%20Mterics.png)

![](Images/Prometheus%20metrics.png)

#### What insight you are getting from this metrics dashboard?

1. You can monitor your API end points.
2. You can monitor dot.net run time related data such as memory etc.
3. You can monitor your docker container by configuring container to emit metrics.
4. You can monitor kubernetes cluster as well.
5. With the metrics, you can set up alerting and take actions to corrective measures.

#### sample logging using Grafana loki

I have used Grafana loki for application logging with serilog. And you will be able to search
logs with trace id and find all the logs related to functionality even when the flow involves
intra service communications.

![](Images/Grafana%20Loki.png)

![](Images/Grafana%20Loki1.png)

Reference:
[Open Telemetry](https://opentelemetry.io/docs/)
[Jaeger](https://www.jaegertracing.io/docs/1.42/)
[ZipKin](https://zipkin.io/)
[Prometheus](https://prometheus.io/docs/concepts/metric_types/)
[Open Telemetry Registry](https://opentelemetry.io/ecosystem/registry)
[Grafana](https://grafana.com/)
