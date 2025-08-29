using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Text;

namespace WebApiCorreosDeCostaRicaLimpio.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CorreosController : ControllerBase
    {


        // POST: CorreosController/Create
        [HttpGet]
        [Route("Autorizacion")]
        public string Create(IFormCollection collection)
        {

            Correos nuevo = new Correos("x", "x", "1", "1", "1");


            return nuevo.GetToken().Result;

        }
        [HttpGet]
        [Route("Provincias")]
        public Dictionary<string, string> Provincias(IFormCollection collection) {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetProvincias();


        }
        [HttpGet]
        [Route("GenerarGuia")]
        public string GenerarGuia(IFormCollection collection)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GenerarGuia();


        }
        [HttpGet]
        [Route("Cantones")]
        public Dictionary<string, string> Cantones(IFormCollection collection, string codProvincia)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetCantones(codProvincia);


        }
        [HttpGet]
        [Route("Distritos")]
        public Dictionary<string, string> Distritos(IFormCollection collection, string codProvincia, string codCanton)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetDistritos(codProvincia, codCanton);


        }

        [HttpGet]
        [Route("Barrios")]
        public List<Dictionary<string, string>> Barrios(IFormCollection collection, string codProvincia, string codCanton, string codDistrito)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetBarrios(codProvincia, codCanton, codDistrito);


        }
        [HttpGet]
        [Route("CodPostal")]
        public string CodigoPostal(IFormCollection collection, string codProvincia, string codCanton, string codDistrito)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetCodigoPostal(codProvincia, codCanton, codDistrito);


        }
        [HttpGet]
        [Route("Tarifa")]
        public Dictionary<string, string> Tarifa(IFormCollection collection, string provinciaOrigen, string cantonOrigen, string ProvinciaDestino, string cantonDestino, int peso)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            return nuevo.GetTarifa(provinciaOrigen,cantonOrigen ,ProvinciaDestino,cantonDestino,peso );


        }

        [HttpGet]
        [Route("RegistroEnvio")]
        public async Task<Dictionary<string, string>> RegistroEnvioAsync()
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");

            byte[] array = new byte[300];

            Request.EnableBuffering();

            Request.Body.Position = 0;

            var rawRequestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            var jobect = JObject.Parse(rawRequestBody);
            string textoPrueba = jobect.GetValue("nombre") != null ? jobect.GetValue("nombre").ToString() : "";
            Dictionary<string, string> test = new Dictionary<string, string>()
            { {"DEST_APARTADO",jobect.GetValue("DEST_APARTADO") != null ? jobect.GetValue("DEST_APARTADO").ToString() : ""},
                {"DEST_DIRECCION",jobect.GetValue("DEST_DIRECCION") != null ? jobect.GetValue("DEST_DIRECCION").ToString() : "" },
                {"DEST_NOMBRE",jobect.GetValue("DEST_NOMBRE") != null ? jobect.GetValue("DEST_NOMBRE").ToString() : "" },
                {"DEST_TELEFONO",jobect.GetValue("DEST_TELEFONO") != null ? jobect.GetValue("DEST_TELEFONO").ToString() : ""},
                {"DEST_ZIP",jobect.GetValue("DEST_ZIP") != null ? jobect.GetValue("DEST_ZIP").ToString() : "" },
                {"ENVIO_ID",jobect.GetValue("ENVIO_ID") != null ? jobect.GetValue("ENVIO_ID").ToString() : ""},
                {"MONTO_FLETE",jobect.GetValue("MONTO_FLETE") != null ? jobect.GetValue("MONTO_FLETE").ToString() : "" },
            {"OBSERVACIONES",jobect.GetValue("OBSERVACIONES") != null ? jobect.GetValue("OBSERVACIONES").ToString() : "" },
                {"PESO",jobect.GetValue("PESO") != null ? jobect.GetValue("PESO").ToString() : "" },
                {"VARIABLE_1",jobect.GetValue("VARIABLE_1") != null ? jobect.GetValue("VARIABLE_1").ToString() : ""},
                {"VARIABLE_3",jobect.GetValue("VARIABLE_3") != null ? jobect.GetValue("VARIABLE_3").ToString() : "" },
                {"VARIABLE_4",jobect.GetValue("VARIABLE_4") != null ? jobect.GetValue("VARIABLE_4").ToString() : "" },
                {"VARIABLE_5",jobect.GetValue("VARIABLE_5") != null ? jobect.GetValue("VARIABLE_5").ToString() : "" },

            {"VARIABLE_6",jobect.GetValue("VARIABLE_6") != null ? jobect.GetValue("VARIABLE_6").ToString() : "" },
                {"VARIABLE_7",jobect.GetValue("VARIABLE_7") != null ? jobect.GetValue("VARIABLE_7").ToString() : "" },
                {"VARIABLE_8",jobect.GetValue("VARIABLE_8") != null ? jobect.GetValue("VARIABLE_8").ToString() : "" },
                {"VARIABLE_9",jobect.GetValue("VARIABLE_9") != null ? jobect.GetValue("VARIABLE_9").ToString() : "" },
                {"VARIABLE_10",jobect.GetValue("VARIABLE_10") != null ? jobect.GetValue("VARIABLE_10").ToString() : "" },
                {"VARIABLE_11",jobect.GetValue("VARIABLE_11") != null ? jobect.GetValue("VARIABLE_11").ToString() : "" },
            {"VARIABLE_12",jobect.GetValue("VARIABLE_12") != null ? jobect.GetValue("VARIABLE_12").ToString() : "" },
                {"VARIABLE_13",jobect.GetValue("VARIABLE_13") != null ? jobect.GetValue("VARIABLE_13").ToString() : "" },
                {"VARIABLE_14",jobect.GetValue("VARIABLE_14") != null ? jobect.GetValue("VARIABLE_14").ToString() : "" },
                {"VARIABLE_15",jobect.GetValue("VARIABLE_15") != null ? jobect.GetValue("VARIABLE_15").ToString() : "" }
            };
                        Dictionary<string, string> sender = new Dictionary<string, string>() { { "direction", jobect.GetValue("direction") != null ? jobect.GetValue("direction").ToString() : "" 
                            }, { "name", jobect.GetValue("name") != null ? jobect.GetValue("name").ToString() : "" 
                            },{ "phone", jobect.GetValue("phone") != null ? jobect.GetValue("phone").ToString() : "" 
                            },{"zip",jobect.GetValue("zip") != null ? jobect.GetValue("zip").ToString() : "" } };

            //return  "este seria el request body" + textoPrueba;
            return nuevo.RegistroEnvio(1,test,sender);


        }

        [HttpGet]
        [Route("Tracking")]
        public Dictionary<string, object> Tracking(IFormCollection collection, string numeroEnvio)
        {
            Correos nuevo = new Correos("x", "x", "1", "1", "1");
            return nuevo.GetTracking(numeroEnvio);


        }

        

    }
}
