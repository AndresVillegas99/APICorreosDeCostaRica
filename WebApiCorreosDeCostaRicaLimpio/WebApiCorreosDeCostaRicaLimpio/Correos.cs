using MySql.Data.MySqlClient;

using System;
using System.Net.Http.Headers;
using System.Security.Cryptography.Xml;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Net;
using System.Web;
using System.Text;
using ZstdSharp.Unsafe;
using Newtonsoft.Json.Linq;
using System.Xml;
using MySqlX.XDevAPI.Relational;
using Mysqlx.Notice;

namespace WebApiCorreosDeCostaRicaLimpio
{
    public class Correos
    {
        private Dictionary<string, string> credentials;
        private Dictionary<string, string> environment;
        private string token;
        private DateTime tokenTimestamp;
        private Dictionary<string, string> proxySettings;
        private List<string> methods = new List<string> { "ccrCodProvincia",
            "ccrCodCanton",
            "ccrCodDistrito",
            "ccrCodBarrio",
            "ccrCodPostal",
            "ccrTarifa",
            "ccrGenerarGuia",
            "ccrRegistroEnvio",
            "ccrMovilTracking"};

        public Correos(string username, string password, string userId, string serviceId, string clientCode, string environment = "production")
        {
            this.credentials = new Dictionary<string, string>()
            {
                { "Username", username },
                { "Password", password },
                { "User_id", userId },
                { "Service_id", serviceId },
                { "Client_code", clientCode }
            };

            this.environment = new Dictionary<string, string>();

            if (environment == "sandbox")
            {
                this.environment["auth_port"] = ""; //Numero de Puerto para autenticar
                this.environment["auth_url"] = ""; //Url de autenticacion
                this.environment["process_url"] = ""; //Url para el WSDL
                this.environment["process_port"] = ""; //Numero de puerto para los procesos WSDL
            }
            else if (environment == "production")
            {
                this.environment["auth_port"] = "";
                this.environment["auth_url"] = "";
                this.environment["process_url"] = "";
                this.environment["process_port"] = "";
            }
        }

        private async Task<string> Auth()
        {
            if (string.IsNullOrEmpty(credentials["Username"]) || string.IsNullOrEmpty(credentials["Password"]))
            {
                return null;
            }



            var body = new
            {
                Username = credentials["Username"],
                Password = credentials["Password"],
                Sistema = "CORPORATIVO"
            };

            using (var client = new HttpClient())
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");


                // client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var response = await client.PostAsync(environment["auth_url"], content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    token = responseContent;
                    tokenTimestamp = DateTime.Now;
                    return token;
                }
                else
                {
                    Console.WriteLine("Authentication failed: " + response.StatusCode);
                    return null;
                }
            }
        }

        public async Task<string> GetToken()
        {
            if (string.IsNullOrEmpty(token) || (DateTime.Now - tokenTimestamp).TotalSeconds > 270)
            {
                return await Auth();
            }
            else
            {
                return token;
            }
        }




        public Dictionary<string, string> GetProvincias()
        {
            //Metodo que solo ocupa llamarse, retorna provincias y sus codigos
            Dictionary<string, string> provincias = new Dictionary<string, string>();
            Dictionary<string, string> parametro = new Dictionary<string, string>();

            var response = request("ccrCodProvincia", parametro);
            string strResponse = "ccrCodProvincia" + "Response";
            string strResult = "ccrCodProvincia" + "Result";
            //Siempre se tiene que crear un documento xml y almacenar la informacion deseada en un NodeList, 
            //la informacion deseada esta debajo de la etiqueta de Provincias
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response);
            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Provincias");

            foreach (XmlElement provincia in nodeList)
            {

                foreach (XmlElement data in provincia)
                {
                    //var data = (Dictionary<string, object>)provincia;
                    //Hay 3 nivel de etiqutas en esta respuesta, se ocupan 2 for each
                    string codigo = data["Codigo"].InnerText;
                    string descripcion = data["Descripcion"].InnerText;
                    provincias.Add(codigo, descripcion);
                }

            }
            return provincias;

        }
        public Dictionary<string, string> GetCantones(string codigoProvincia)
        {
            //metodo que ocupa minimo un codigo de provincia valido, retorna codigos de cantons y nombres
            Dictionary<string, string> cantones = new Dictionary<string, string>();
            //Se ingresa los valores a un diccionario de datos para cuando se realize la accion SOAP estos se puedan pasar como parametros
            var replacements = new Dictionary<string, string>
            {
                { "%CodProvincia%", codigoProvincia }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%CodProvincia%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                }
            };




            if (CheckParameters(replacements, dataTypes, "get_cantones"))
            {
                string strResponse = "ccrCodCanton" + "Response";
                string strResult = "ccrCodCanton" + "Result";
                var response = request("ccrCodCanton", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Cantones");
                foreach (XmlElement canton in nodeList)
                {
                    //var data = (Dictionary<string, object>)obj;
                    foreach (XmlElement data in canton)
                    {
                        string codigo = data["Codigo"].InnerText;
                        string descripcion = data["Descripcion"].InnerText;
                        cantones.Add(codigo, descripcion);
                    }

                }
            }
            return cantones;
        }

        public Dictionary<string, string> GetDistritos(string codigoProvincia, string codigoCanton)
        {
            //metodo que retorna codigos de distritos y nombres, ocupa codigo de provincia y canton valido
            Dictionary<string, string> distritos = new Dictionary<string, string>();
            var replacements = new Dictionary<string, string>
            {
                { "%CodProvincia%", codigoProvincia },
                { "%CodCanton%", codigoCanton }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%CodProvincia%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                },
                { "%CodCanton%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                }
            };
            if (CheckParameters(replacements, dataTypes, "get_distritos"))
            {
                string strResponse = "ccrCodDistrito" + "Response";
                string strResult = "ccrCodDistrito" + "Result";
                var response = request("ccrCodDistrito", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Distritos");
                foreach (XmlElement distrito in nodeList)
                {
                    foreach (XmlElement data in distrito)
                    {
                        //var data = (Dictionary<string, object>)obj;
                        string codigo = data["Codigo"].InnerText;
                        string descripcion = data["Descripcion"].InnerText;
                        distritos.Add(codigo, descripcion);
                    }
                }
            }
            return distritos;
        }

        public List<Dictionary<string, string>> GetBarrios(string codigoProvincia, string codigoCanton, string codigoDistrito)
        {
            //metodo que retorna codigo de barrios y nombres, ocupa codigo de provincia, canton y distrito validos
            List<Dictionary<string, string>> barrios = new List<Dictionary<string, string>>();
            var replacements = new Dictionary<string, string>
            {
                { "%CodProvincia%", codigoProvincia },
                { "%CodCanton%", codigoCanton },
                { "%CodDistrito%", codigoDistrito }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%CodProvincia%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                },
                { "%CodCanton%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                },
                { "%CodDistrito%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                }
            };
            if (CheckParameters(replacements, dataTypes, "get_barrios"))
            {
                string strResponse = "ccrCodBarrio" + "Response";
                string strResult = "ccrCodBarrio" + "Result";
                var response = request("ccrCodBarrio", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Barrios");
                foreach (XmlElement barrioInfo in nodeList)
                {
                    //var data = (Dictionary<string, object>)obj;
                    foreach (XmlElement data in barrioInfo)
                    {
                        string codigo = (string)data["CodBarrio"].InnerText;
                        string sucursal = (string)data["CodSucursal"].InnerText;
                        string nombre = (string)data["Nombre"].InnerText;
                        Dictionary<string, string> barrio = new Dictionary<string, string>
                    {
                        { "codigo", codigo },
                        { "nombre", nombre },
                        { "sucursal", sucursal }
                    };
                        barrios.Add(barrio);
                    }


                }
            }
            return barrios;
        }

        public string GetCodigoPostal(string codigoProvincia, string codigoCanton, string codigoDistrito)
        {
            //Retorna el codigo postal de una localizacion, se ocupa codigo de provincia, canton y distrito
            string codigoPostal = "";
            var replacements = new Dictionary<string, string>
            {
                { "%CodProvincia%", codigoProvincia },
                { "%CodCanton%", codigoCanton },
                { "%CodDistrito%", codigoDistrito }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%CodProvincia%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                },
                { "%CodCanton%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                },
                { "%CodDistrito%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                }
            };
            if (CheckParameters(replacements, dataTypes, "get_codigo_postal"))
            {
                string strResponse = "ccrCodPostal" + "Response";
                string strResult = "ccrCodPostal" + "Result";
                var response = request("ccrCodPostal", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList nodeList = xmlDoc.GetElementsByTagName("CodPostal");
                codigoPostal = nodeList[0].InnerText;
            }
            return codigoPostal;
        }

        public Dictionary<string, string> GetTarifa(string provinciaOrigen, string cantonOrigen, string provinciaDestino, string cantonDestino,  int peso)
        {
            //Da un aproximado de tariba, se ocupa provincia, canton de destino y el peso del artiuclo.
            Dictionary<string, string> tarifa = new Dictionary<string, string>();
            var replacements = new Dictionary<string, string>
            {
                { "%ProvinciaOrigen%", provinciaOrigen },
                { "%CantonOrigen%", cantonOrigen },
                { "%ProvinciaDestino%", provinciaDestino },
                { "%CantonDestino%", cantonDestino },
                { "%Servicio%", credentials["Service_id"] },
                { "%Peso%", peso.ToString() }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%ProvinciaOrigen%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                },
                { "%CantonOrigen%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                },
                { "%ProvinciaDestino%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 1 }
                    }
                },
                { "%CantonDestino%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 2 }
                    }
                },
                { "%Servicio%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 5 }
                    }
                },
                { "%Peso%", new Dictionary<string, object>
                    {
                        { "type", "numeric" }
                    }
                }
            };
            if (CheckParameters(replacements, dataTypes, "get_tarifa"))
            {
                string strResponse = "ccrTarifa" + "Response";
                string strResult = "ccrTarifa" + "Result";
                var response = request("ccrTarifa", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList tarifaInfo = xmlDoc.GetElementsByTagName("MontoTarifa");
                XmlNodeList descuentoInfo = xmlDoc.GetElementsByTagName("Descuento");
                XmlNodeList impuestoInfo = xmlDoc.GetElementsByTagName("Impuesto");

                XmlNodeList codResInfo = xmlDoc.GetElementsByTagName("MensajeRespuesta");

                tarifa.Add("tarifa", tarifaInfo[0].InnerText);
                tarifa.Add("descuento", descuentoInfo[0].InnerText);
                tarifa.Add("impuesto", impuestoInfo[0].InnerText);
                tarifa.Add("MensajeRespuesta", codResInfo[0].InnerText);
            }
            return tarifa;
        }

        public string GenerarGuia()
        {
            //Genera una guia que permite registrar un envio
            Dictionary<string, string> parametro = new Dictionary<string, string>();
            string strResponse = "ccrGenerarGuia" + "Response";
            string strResult = "ccrGenerarGuia" + "Result";

            var response = request("ccrGenerarGuia", parametro);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response);
            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("NumeroEnvio");
            string numeroEnvio = nodeList[0].InnerText;
            return numeroEnvio;

        }
        

        public Dictionary<string, string> RegistroEnvio(int orderId, Dictionary<string, string> destino, Dictionary<string, string> sender)
        {
           //Se encarga de registrar un envio para que este tambien pueda empezar a rastrearse
            string status = "";
            Dictionary<string, string> replacements = new Dictionary<string, string>
            {
                ["%Cliente%"] = credentials.ContainsKey("Client_code") ? credentials["Client_code"] : "",
                ["%COD_CLIENTE%"] = credentials.ContainsKey("Client_code") ? credentials["Client_code"] : "",
                ["%DEST_APARTADO%"] = destino.ContainsKey("DEST_APARTADO") ? destino["DEST_APARTADO"] : "",
                ["%DEST_DIRECCION%"] = destino.ContainsKey("DEST_DIRECCION") ? destino["DEST_DIRECCION"] : "",
                ["%DEST_NOMBRE%"] = destino.ContainsKey("DEST_NOMBRE") ? destino["DEST_NOMBRE"] : "",
                ["%DEST_TELEFONO%"] = destino.ContainsKey("DEST_TELEFONO") ? destino["DEST_TELEFONO"] : "",
                ["%DEST_ZIP%"] = destino.ContainsKey("DEST_ZIP") ? destino["DEST_ZIP"] : "",
                ["%ENVIO_ID%"] = destino.ContainsKey("ENVIO_ID") ? destino["ENVIO_ID"] : "",
                ["%FECHA_ENVIO%"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                ["%MONTO_FLETE%"] = destino.ContainsKey("MONTO_FLETE") ? destino["MONTO_FLETE"] : "",
                ["%OBSERVACIONES%"] = destino.ContainsKey("OBSERVACIONES") ? destino["OBSERVACIONES"] : "",
                ["%PESO%"] = destino.ContainsKey("PESO") ? destino["PESO"] : "",
                ["%SERVICIO%"] = credentials.ContainsKey("Service_id") ? credentials["Service_id"] : "",
                ["%USUARIO_ID%"] = credentials["User_id"].ToString(),
                ["%VARIABLE_1%"] = !string.IsNullOrEmpty(destino["VARIABLE_1"]) ? destino["VARIABLE_1"] : "",
                ["%VARIABLE_3%"] = !string.IsNullOrEmpty(destino["VARIABLE_3"]) ? destino["VARIABLE_3"] : "",
                ["%VARIABLE_4%"] = !string.IsNullOrEmpty(destino["VARIABLE_4"]) ? destino["VARIABLE_4"] : "",
                ["%VARIABLE_5%"] = !string.IsNullOrEmpty(destino["VARIABLE_5"]) ? destino["VARIABLE_5"] : "",
                ["%VARIABLE_6%"] = !string.IsNullOrEmpty(destino["VARIABLE_6"]) ? destino["VARIABLE_6"] : "",
                ["%VARIABLE_7%"] = !string.IsNullOrEmpty(destino["VARIABLE_7"]) ? destino["VARIABLE_7"] : "",
                ["%VARIABLE_8%"] = !string.IsNullOrEmpty(destino["VARIABLE_8"]) ? destino["VARIABLE_8"] : "",
                ["%VARIABLE_9%"] = !string.IsNullOrEmpty(destino["VARIABLE_9"]) ? destino["VARIABLE_9"] : "",
                ["%VARIABLE_10%"] = !string.IsNullOrEmpty(destino["VARIABLE_10"]) ? destino["VARIABLE_10"] : "",
                ["%VARIABLE_11%"] = !string.IsNullOrEmpty(destino["VARIABLE_11"]) ? destino["VARIABLE_11"] : "",
                ["%VARIABLE_12%"] = !string.IsNullOrEmpty(destino["VARIABLE_12"]) ? destino["VARIABLE_12"] : "",
                ["%VARIABLE_13%"] = !string.IsNullOrEmpty(destino["VARIABLE_13"]) ? destino["VARIABLE_13"] : "",
                ["%VARIABLE_14%"] = !string.IsNullOrEmpty(destino["VARIABLE_14"]) ? destino["VARIABLE_14"] : "",
                ["%VARIABLE_15%"] = !string.IsNullOrEmpty(destino["VARIABLE_15"]) ? destino["VARIABLE_15"] : "",

      ["%SEND_DIRECCION%"] = sender["direction"],
                ["%SEND_NOMBRE%"] = sender["name"],
                ["%SEND_TELEFONO%"] = sender["phone"],
                ["%SEND_ZIP%"] = sender["zip"]
            };
            var data_types = new Dictionary<string, Dictionary<string, object>>
            {
                { "%Cliente%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 10 }
                    }
                },
                { "%COD_CLIENTE%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 20 }
                    }
                },
                { "%FECHA_ENVIO%", new Dictionary<string, object>
                    {
                        { "type", "datetime" }
                    }
                },
                { "%ENVIO_ID%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 25 }
                    }
                },
                { "%SERVICIO%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 5 }
                    }
                },
                { "%MONTO_FLETE%", new Dictionary<string, object>
                    {
                         { "type", "numeric" }
                    }
                },
                { "%DEST_NOMBRE%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 200 }
                    }
                },
                { "%DEST_DIRECCION%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 500 }
                    }
                },
                { "%DEST_TELEFONO%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 15 }
                    }
                },
                { "%DEST_APARTADO%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 20 }
                    }
                },
                { "%DEST_ZIP%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 8 }
                    }
                },
                { "%SEND_NOMBRE%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 200 }
                    }
                },
                { "%SEND_DIRECCION%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 500 }
                    }
                },
                { "%SEND_ZIP%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 8 }
                    }
                },
                { "%SEND_TELEFONO%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 15 }
                    }
                },
                { "%OBSERVACIONES%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 200 }
                    }
                },
                { "%USUARIO_ID%", new Dictionary<string, object>
                    {
                           { "type", "numeric" }
                    }
                },
                { "%PESO%", new Dictionary<string, object>
                    {
                           { "type", "numeric" }
                    }
                },
                { "%VARIABLE_1%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 10 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_3%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 1 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_4%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 100 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_5%", new Dictionary<string, object>
                    {
                           { "type", "numeric" },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_6%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 2 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_7%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 1 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_8%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 10 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_9%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 1 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_10%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 1 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_11%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 1 },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_12%", new Dictionary<string, object>
                    {
                           { "type", "numeric" },
                    { "optional", true}
                    }
                },
                { "%VARIABLE_13%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 50 },
                    { "optional", true}
                    }
                },
                 { "%VARIABLE_14%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 50 },
                    { "optional", true}
                    }
                },
                  { "%VARIABLE_15%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 50 },
                    { "optional", true}
                    }
                },
                   { "%VARIABLE_16%", new Dictionary<string, object>
                    {
                           { "type", "string" },
                        { "length", 10 },
                    { "optional", true}
                    }
                }


            };

            Dictionary<string, string> respuestaRegistro = new Dictionary<string, string>();
            {
                if (CheckParameters(replacements, data_types, "Registro_Envio"))
                {
                    var response = request("ccrRegistroEnvio", replacements);
                    if (response is object && (response) != null)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(response);
                        XmlNodeList nodeMensajeRes = xmlDoc.GetElementsByTagName("MensajeRespuesta");
                        XmlNodeList nodeCodigoRes = xmlDoc.GetElementsByTagName("CodRespuesta");

                        respuestaRegistro.Add("MensajeRespuesta", nodeMensajeRes[0].InnerText);
                        respuestaRegistro.Add("CodigoRespuesta", nodeCodigoRes[0].InnerText);
                        return respuestaRegistro;
                    }
                    else if (response != null)
                    {
                        Console.WriteLine($"CodRespuesta: {response}, Args: {this.CleanSoapFieldsToParameters(replacements)}");
                    }
                    else
                    {
                        Console.WriteLine($"Args: {CleanSoapFieldsToParameters(replacements)}");
                    }
                }
               
                return respuestaRegistro; // Assuming status is a Dictionary<string, object> defined elsewhere
            }
   
        }
    
        

        public Dictionary<string, object> GetTracking(string guideNumber)
        {
            //metodo para conseguir el tracking de un pedido
            Dictionary<string, object> data = new Dictionary<string, object>();
            var replacements = new Dictionary<string, string>
            {
                { "%NumeroEnvio%", guideNumber }
            };
            var dataTypes = new Dictionary<string, Dictionary<string, object>>
            {
                { "%NumeroEnvio%", new Dictionary<string, object>
                    {
                        { "type", "string" },
                        { "length", 50 }
                    }
                }
            };
            if (CheckParameters(replacements, dataTypes, "get_tracking"))
            {
                string strResponse = "ccrMovilTracking" + "Response";
                string strResult = "ccrMovilTracking" + "Result";
                var response = request("ccrMovilTracking", replacements);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList nodeEncabezado = xmlDoc.GetElementsByTagName("Encabezado");


                var encabezado = nodeEncabezado[0];
                data.Add("encabezado", new Dictionary<string, object>
                {
                    { "estado", encabezado["Estado"].InnerText },
                    { "fecha-recepcion", encabezado["FechaRecepcion"].InnerText },
                    { "destinatario", encabezado["NombreDestinatario"].InnerText }
                });
                List<Dictionary<string, object>> eventos = new List<Dictionary<string, object>>();
                XmlNodeList nodeEvento = xmlDoc.GetElementsByTagName("Eventos");

                foreach (XmlElement evento in nodeEvento)
                {
                    //var item = (Dictionary<string, object>)obj;
                    foreach (XmlNode eventNode in evento) { 
                    eventos.Add(new Dictionary<string, object>
                    {
                        { "evento", eventNode["Evento"].InnerText },
                        { "fecha-hora", eventNode["FechaHora"].InnerText },
                        { "unidad", eventNode["Unidad"].InnerText }
                    });
                    }
                }
                data.Add("eventos", eventos);
            }
            return data;
        }

        private bool CheckParameters(Dictionary<string, string> replacements, Dictionary<string, Dictionary<string, object>> dataTypes, string method = "")
        {
            //Este metodo verifica los parametros presentados, asignandoles su valor numero, string, etc.
            bool tryRegister = true;
            replacements = CleanSoapFieldsToParameters(replacements);
            dataTypes = CleanSoapFieldsToParameters(dataTypes);
            foreach (var field in replacements)
            {
                var fieldParams = dataTypes[field.Key];
                if (string.IsNullOrEmpty(field.Value))
                {
                    if (fieldParams.ContainsKey("optional") && (bool)fieldParams["optional"])
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Empty parameter \"{field.Key}\" called from \"{method}\".");
                        tryRegister = false;
                    }
                }
                if (fieldParams["type"] == "string")
                {
                    int maxLength = (int)fieldParams["length"];
                    int paramLength = field.Value.Length;
                    if (paramLength > maxLength)
                    {
                        Console.WriteLine($"\"{field.Key}\" cannot exceed {maxLength} characters. Given: {paramLength}, \"{field.Value}\" called from \"{method}\"");
                        tryRegister = false;
                    }
                }
                if (fieldParams["type"] == "numeric")
                {
                    if (!long.TryParse(field.Value, out long result))
                    {
                        Console.WriteLine($"Bad \"{field.Key}\" Given: \"{field.Value}\" called from \"{method}\"");
                        tryRegister = false;
                    }
                }
            }
            return tryRegister;
        }

        private Dictionary<string, string> CleanSoapFieldsToParameters(Dictionary<string, string> replacements)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach (var field in replacements)
            {
                string fieldName = field.Key.Replace("%", "");
                data[fieldName] = field.Value;
            }
            return data;
        }

        private Dictionary<string, Dictionary<string, object>> CleanSoapFieldsToParameters(Dictionary<string, Dictionary<string, object>> dataTypes)
        {
            //Limpia los tipos de datos que son cada uno de los atributos presentados
            Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>();
            foreach (var field in dataTypes)
            {
                string fieldName = field.Key.Replace("%", "");
                data[fieldName] = field.Value;
            }
            return data;
        }
        private string request(string method, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(method))
            {
                return null;
            }
            if (!methods.Contains(method))
            {
                return null;
            }
            //Crea una instancia web de tipo xml para poder comunicarse con SOAP
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.Authorization] = GetToken().Result;
            client.Headers[HttpRequestHeader.ContentType] = "text/xml; charset=utf-8";
            client.Headers["SOAPAction"] = "http://tempuri.org/IwsAppCorreos/" + method;

            string soapFields = getSoapFields(method, replacements);
            byte[] postData = Encoding.UTF8.GetBytes(soapFields);

            try
            {
                //Envia la informacion para realizar la accion POST con SOAPAction
                byte[] responseData = client.UploadData(environment["process_url"], "POST", postData);
                string response = Encoding.UTF8.GetString(responseData);

                response = System.Text.RegularExpressions.Regex.Replace(response, "(<\\/?)\\w+:([^>]*>)", "$1$2");

                // Parse the XML response
                // You can use XML libraries like XmlDocument or XDocument to parse the XML response here
                // Example: XmlDocument xmlDoc = new XmlDocument();
                //          xmlDoc.LoadXml(response);
                //          XmlNodeList nodeList = xmlDoc.GetElementsByTagName("yourNodeName");


                string strResponse = method + "Response";
                string strResult = method + "Result";
              
                // responseValue = nodeList[0][strResponse][strResult].InnerText;
                return response;
               
            }
            catch (WebException ex)
            {
                Console.WriteLine("Error in service query: " + ex.Message);
                return null;
            }
        }

        private string getSoapFields(string method, Dictionary<string, string> replacements)
        {
            Dictionary<string, string> fields = new Dictionary<string, string> //Diccionario de body para las conexiones SOAP
            {
                { "ccrCodProvincia", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrCodProvincia/>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>" },
                { "ccrCodCanton", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrCodCanton>\r\n         <tem:CodProvincia>%CodProvincia%</tem:CodProvincia>\r\n      </tem:ccrCodCanton>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>" },
                { "ccrCodDistrito", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrCodDistrito>\r\n     <tem:CodProvincia>%CodProvincia%</tem:CodProvincia>\r\n      <tem:CodCanton>%CodCanton%</tem:CodCanton>\r\n      </tem:ccrCodDistrito>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>"},
                {"ccrCodBarrio","<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrCodBarrio>\r\n        <tem:CodProvincia>%CodProvincia%</tem:CodProvincia>\r\n   <tem:CodCanton>%CodCanton%</tem:CodCanton>\r\n    <tem:CodDistrito>%CodDistrito%</tem:CodDistrito>\r\n      </tem:ccrCodBarrio>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>" },
                { "ccrCodPostal" , "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrCodPostal>\r\n     <tem:CodProvincia>%CodProvincia%</tem:CodProvincia>\r\n       <tem:CodCanton>%CodCanton%</tem:CodCanton>\r\n    <tem:CodDistrito>%CodDistrito%</tem:CodDistrito>\r\n      </tem:ccrCodPostal>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>"},
                { "ccrGenerarGuia", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrGenerarGuia/>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>"},
                { "ccrMovilTracking", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrMovilTracking>\r\n  <tem:NumeroEnvio>%NumeroEnvio%</tem:NumeroEnvio>\r\n      </tem:ccrMovilTracking>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>"},
                { "ccrTarifa", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\" xmlns:wsap=\"http://schemas.datacontract.org/2004/07/wsAppCorreos\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrTarifa>\r\n         <tem:reqTarifa>\r\n            <wsap:CantonDestino>%CantonDestino%</wsap:CantonDestino>\r\n            <wsap:CantonOrigen>%CantonOrigen%</wsap:CantonOrigen>\r\n        <wsap:Peso>%Peso%</wsap:Peso>\r\n            <wsap:ProvinciaDestino>%ProvinciaDestino%</wsap:ProvinciaDestino>\r\n            <wsap:ProvinciaOrigen>%ProvinciaOrigen%</wsap:ProvinciaOrigen>\r\n            <wsap:Servicio>%Servicio%</wsap:Servicio>\r\n         </tem:reqTarifa>\r\n      </tem:ccrTarifa>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>"},
                { "ccrRegistroEnvio", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\" xmlns:wsap=\"http://schemas.datacontract.org/2004/07/wsAppCorreos\">\r\n   <soapenv:Header/>\r\n   <soapenv:Body>\r\n      <tem:ccrRegistroEnvio>\r\n  <tem:ccrReqEnvio>\r\n    <wsap:Cliente>%Cliente%</wsap:Cliente>\r\n    <wsap:Envio>\r\n       <wsap:COD_CLIENTE>%COD_CLIENTE%</wsap:COD_CLIENTE>\r\n    <wsap:DEST_APARTADO>%DEST_APARTADO%</wsap:DEST_APARTADO>\r\n         <wsap:DEST_DIRECCION>%DEST_DIRECCION%</wsap:DEST_DIRECCION>\r\n      <wsap:DEST_NOMBRE>%DEST_NOMBRE%</wsap:DEST_NOMBRE>\r\n        <wsap:DEST_TELEFONO>%DEST_TELEFONO%</wsap:DEST_TELEFONO>\r\n        <wsap:DEST_ZIP>%DEST_ZIP%</wsap:DEST_ZIP>\r\n        <wsap:ENVIO_ID>%ENVIO_ID%</wsap:ENVIO_ID>\r\n      <wsap:FECHA_ENVIO>%FECHA_ENVIO%</wsap:FECHA_ENVIO>\r\n     <wsap:MONTO_FLETE>%MONTO_FLETE%</wsap:MONTO_FLETE>\r\n    <wsap:OBSERVACIONES>%OBSERVACIONES%</wsap:OBSERVACIONES>\r\n    <wsap:PESO>%PESO%</wsap:PESO>\r\n           <wsap:SEND_DIRECCION>%SEND_DIRECCION%</wsap:SEND_DIRECCION>\r\n       <wsap:SEND_NOMBRE>%SEND_NOMBRE%</wsap:SEND_NOMBRE>\r\n        <wsap:SEND_TELEFONO>%SEND_TELEFONO%</wsap:SEND_TELEFONO>\r\n    <wsap:SEND_ZIP>%SEND_ZIP%</wsap:SEND_ZIP>\r\n      <wsap:SERVICIO>%SERVICIO%</wsap:SERVICIO>\r\n    <wsap:USUARIO_ID>%USUARIO_ID%</wsap:USUARIO_ID>\r\n         <wsap:VARIABLE_1>%VARIABLE_1%</wsap:VARIABLE_1>\r\n   <wsap:VARIABLE_10>%VARIABLE_10%</wsap:VARIABLE_10>\r\n         <wsap:VARIABLE_11>%VARIABLE_11%</wsap:VARIABLE_11>\r\n        <wsap:VARIABLE_12>%VARIABLE_12%</wsap:VARIABLE_12>\r\n   <wsap:VARIABLE_13>%VARIABLE_13%</wsap:VARIABLE_13>\r\n   <wsap:VARIABLE_14>%VARIABLE_14%</wsap:VARIABLE_14>\r\n      <wsap:VARIABLE_15>%VARIABLE_15%</wsap:VARIABLE_15>\r\n      <wsap:VARIABLE_16>%VARIABLE_16%</wsap:VARIABLE_16>\r\n      <wsap:VARIABLE_3>%VARIABLE_3%</wsap:VARIABLE_3>\r\n    <wsap:VARIABLE_4>%VARIABLE_4%</wsap:VARIABLE_4>\r\n     <wsap:VARIABLE_5>%VARIABLE_5%</wsap:VARIABLE_5>\r\n     <wsap:VARIABLE_6>%VARIABLE_6%</wsap:VARIABLE_6>\r\n    <wsap:VARIABLE_7>%VARIABLE_7%</wsap:VARIABLE_7>\r\n        <wsap:VARIABLE_8>%VARIABLE_8%</wsap:VARIABLE_8>\r\n     <wsap:VARIABLE_9>%VARIABLE_9%</wsap:VARIABLE_9>\r\n            </wsap:Envio>\r\n         </tem:ccrReqEnvio>\r\n      </tem:ccrRegistroEnvio>\r\n   </soapenv:Body>\r\n</soapenv:Envelope>" }
                // Add other soap fields here
            };

            string field = fields[method];

            foreach (KeyValuePair<string, string> replacement in replacements)
            {
                field = field.Replace(replacement.Key, replacement.Value);
            }

            // Remove empty replacements
            field = System.Text.RegularExpressions.Regex.Replace(field, "(%[0-9a-zA-z_]+%)", "");

            return field;
        }
    }
}