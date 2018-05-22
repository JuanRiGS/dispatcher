using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ConvenioDispatcher
{
    [DataContract]
    [Serializable]
    public class factura
    {
        [Required]
        [DataMember(Name = "idFactura")]
        [Description("")]
        public int idFactura { get; set; }

        [Required]
        [DataMember(Name = "valorFactura")]
        [Description("")]
        public double valorFactura { get; set; }
    }
}