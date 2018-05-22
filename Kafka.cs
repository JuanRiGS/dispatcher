using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using Newtonsoft.Json;
namespace ConvenioDispatcher
{

    public class Kafka
    {

        private static string kafkaLectura = "localhost:9092";
        private static string kafkaEscritura = "localhost:9093";
        private static string kafkaTopicLectura = "broker-replicated";
        private static string kafkaTopicEscritura = "broker-replicated2";
        
        private static string requestSOAP = "<x:Envelope xmlns:x='http://schemas.xmlsoap.org/soap/envelope/' xmlns:sch='http://www.servicios.co/pagos/schemas'><x:Header/><x:Body><sch:ReferenciaFactura><sch:referenciaFactura>{0}</sch:referenciaFactura></sch:ReferenciaFactura></x:Body></x:Envelope>";
        private static string requestSOAPPOST = "<x:Envelope xmlns:x='http://schemas.xmlsoap.org/soap/envelope/' xmlns:sch='http://www.servicios.co/pagos/schemas'><x:Header/><x:Body><sch:Pago><sch:referenciaFactura><sch:referenciaFactura>{0}</sch:referenciaFactura></sch:referenciaFactura><sch:totalPagar>{1}</sch:totalPagar></sch:Pago></x:Body></x:Envelope>";

        public static string Consumer()
        {
            var kafkamessage = "";
            var kafkaconf = new Dictionary<string, object>
            {
                { "group.id", "test-consumer-group" },
                { "bootstrap.servers", kafkaLectura },
                { "auto.commit.interval.ms", 5000 },
                { "auto.offset.reset", "earliest" }
            };

            using (var consumer = new Consumer<Null, string>(kafkaconf, null, new StringDeserializer(Encoding.UTF8)))
            {
                consumer.OnMessage += (_, msg)
                  => kafkamessage = msg.Value; 

                consumer.OnError += (_, error)
                  => kafkamessage = error.ToString();

                consumer.OnConsumeError += (_, msg)
                  => kafkamessage = msg.Error.ToString();

                consumer.Subscribe(kafkaTopicLectura);

                while (true)
                {
                    consumer.Poll(TimeSpan.FromMilliseconds(100));
                    if (!string.IsNullOrWhiteSpace(kafkamessage))
                    {
                        Console.WriteLine($"Message receive from Kafka: {kafkamessage}");

                        Procesar(kafkamessage);
                        kafkamessage = "";
                    }
                }
            }
        }

        private static void Procesar(string message)
        {
            //Comunicarse con el routing para saber datos de conexion
            obtenerRoutingAsync(message);

        }

        private static async Task obtenerRoutingAsync(string message)
        {

            string json = @"{
                            'id': '05',
                            'name': 'otro',
                            'type': 'SOAP',
                            'operationservice': 'pagar',
                            'url': 'http://35.185.30.69:7070/w1-soap-svr/PagosServiceService',
                            'operation': '01'
                            }";


            var client = new HttpClient();
            var service = await client.GetAsync("http://localhost:9002/api/Routing/getAgreementrouting/" + message);
            string result = "";
            if (service.IsSuccessStatusCode)
            {
                result = await service.Content.ReadAsStringAsync();
            }

            var objRouting = JsonConvert.DeserializeObject<Responserouting>(result);

            //00 es consulta de factura5
            if (objRouting.Data.Operation == "00")
            {
                if(objRouting.Data.Type == "rest")
                {
                    procesarGETAsyncREST(objRouting);
                }
                else
                {
                    procesarGETAsyncSOAP(objRouting);
                }
            }
            else if(objRouting.Data.Operation == "01")
            {
                if(objRouting.Data.Type == "rest")
                {
                    procesarPOSTAsyncRest(objRouting);
                }
                else
                {
                    procesarPOSTAsyncSOAP(objRouting);
                }
            }

        }

         private static async Task procesarGETAsyncREST(Responserouting message)
        {
            var client = new HttpClient();
            var service = await client.GetAsync(message.Data.Url);
            string result = "";
            if (service.IsSuccessStatusCode)
            {
                result = await service.Content.ReadAsStringAsync();
            }
            respuesta(result);         
        }

        private static async Task procesarPOSTAsyncRest(Responserouting message)
        {

            var client = new HttpClient();
            factura objFactura = new factura();
            objFactura.idFactura = message.Data.InvoiceId;
            objFactura.valorFactura = 15000;

            var json = JsonConvert.SerializeObject(objFactura);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(message.Data.Url + message.Data.OperationService + "/" + message.Data.InvoiceId),
                Content = new StringContent(json,Encoding.UTF8,"application/json")
            };

            var service = await client.SendAsync(request);
           
            string result = "";
            if (service.IsSuccessStatusCode)
            {
                result = await service.Content.ReadAsStringAsync();
            }
            respuesta(result);         
        }

         private static async Task procesarGETAsyncSOAP(Responserouting message)
        {
            var request = requestSOAP.Replace("{0}",message.Data.InvoiceId.ToString());
            var result = "";
            using (var client = new HttpClient())
            {
                var action = message.Data.OperationService;
                var newUrl = message.Data.Url;
                client.DefaultRequestHeaders.Add("SOAPAction", action);
                var content = new StringContent(request,Encoding.UTF8,"text/xml");
                using (var response = await client.PostAsync(message.Data.Url,content))
                {
                    result= await response.Content.ReadAsStringAsync();
                }

            }
          
            respuesta(result);    
            
                 
        }

        private static async Task procesarPOSTAsyncSOAP(Responserouting message)
        {
            var request = requestSOAPPOST.Replace("{0}",message.Data.InvoiceId.ToString()).Replace("{1}","12000");
            var result = "";
            using (var client = new HttpClient())
            {
                var action = message.Data.OperationService;
                var newUrl = message.Data.Url;
                client.DefaultRequestHeaders.Add("SOAPAction", action);
                var content = new StringContent(request,Encoding.UTF8,"text/xml");
                using (var response = await client.PostAsync(message.Data.Url,content))
                {
                    result= await response.Content.ReadAsStringAsync();
                }

            }
          
            respuesta(result);    
            
                 
        }

        private static  void respuesta(string json)
        {
            Dictionary<string, object> producerConfigEnvio;  
            producerConfigEnvio = new Dictionary<string, object> { { "bootstrap.servers", kafkaEscritura } };

            using (var producer = new Producer<Null, string>(producerConfigEnvio,null, new StringSerializer(Encoding.UTF8)))
            {
                var dr = producer.ProduceAsync(kafkaTopicEscritura, null, json).Result;
                Console.WriteLine($"Message send to kafka: {json}");
            }
        }

    }

}