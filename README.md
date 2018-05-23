# dispatcher
implemetacion dispacher para la orquestacion del servicios de pagos de servicios publicos

El componente  ConvenioDispacher esta basado en el patrón Massage Dispacher, el cual se encarga de estar revisando en Kafka si hay mensajes para procesar, una vez se realiza la captura del mensaje se envía a un servicio routing, que se encarga de indicar cuál es el servicio que debemos consumir para el procesamiento del mensaje, una vez tenemos identificado el convenio se envía la petición, una vez obtenemos la respuesta es trasmitida a Kafka.
