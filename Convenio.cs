using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using dispatcher.Common;

namespace ConvenioDispatcher
{
    
    public class Convenio
    {



        static void Main(string[] args)
        {
            Console.WriteLine("Dispatcher Started");
            Kafka.Consumer();
        }
    }

}