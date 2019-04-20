namespace CSharp.Politica.PrfOsascoTask
{
    using Data;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class ViagensXml
    {
        public int Nro_Processo { get; set; }
        public string Destino { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }
        public decimal Valor { get; set; }
        public string Motivo { get; set; }
        public string Observacao { get; set; }
        public string Orgao { get; set; }
        public string Unidade { get; set; }
    }

    public class ViagensCsv
    {
        public int Nro_Processo { get; set; }
        public string Destino { get; set; }
        public decimal Valor { get; set; }
        public string Motivo { get; set; }
        public string Observacao { get; set; }
        public string Nome { get; set; }
        public string Cargo { get; set; }
    }

    public class GroupNameTree
    {
        public string Name { get; set; }
        public List<GroupNameTree> Childs = new List<GroupNameTree>();

        public GroupNameTree(string name)
        {
            this.Name = name;
        }

        public static explicit operator GroupNameTree(DictionaryTree<string, string> dictionaryTree)
        {
            var ret = new GroupNameTree(dictionaryTree.Data);

            foreach (var child in dictionaryTree)
            {
                ret.Childs.Add((GroupNameTree)child.Value);
            }

            return ret;
        }
    }

    public class DataPrfOsasco
    {
        public DateTime Date { get; set; }
        public DictionaryTree<string, string> Group { get; set; }
        public decimal Value { get; set; }
    }

    public class Program
    {
        private static readonly string[] rootPath = new string[] { "Política", "Prefeituras", "Osasco" };
        private static readonly Guid OsascoGroupId = new Guid("0AA16E58-12C8-4BB5-9F18-F8BDB9914259");
        public static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;
            string digito;
            string tempCnpj;
            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;
            tempCnpj = cnpj.Substring(0, 12);
            soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cnpj.EndsWith(digito);
        }

        public static bool IsCpf(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;
            tempCpf = cpf.Substring(0, 9);
            soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cpf.EndsWith(digito);
        }

        private static string treatIndex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToUpper();
        }

        public static void BulkInsertViagens()
        {
            Func<string, string> treatIndexLocal = treatIndex;

            for (var year = 2018; year >= 0; year--)
            {
                var count = 100;
                while (count >= 0)
                {
                    count--;
                    try
                    {
                        GetViagens(
                            year,
                            out List<DataPrfOsasco> datas,
                            out DictionaryTree<string, string> groups,
                            treatIndexLocal);
                        //GroupBulkInsertByName(groups);
                        //DataBulkInsert(datas, treatIndex);
                        break;
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(30 * 60 * 1000);
                    }
                }
            }
        }

        public static void BulkInsertDespesas()
        {
            Func<string, string> treatIndexLocal = treatIndex;

            for (var year = 2018; year >= 0; year--)
            {
                for (var month = 12; month >= 1; month--)
                {
                    var count = 100;
                    while (count >= 0)
                    {
                        count--;
                        try
                        {
                            GetDespesas(
                                year, 
                                month, 
                                out List<DataPrfOsasco> datas, 
                                out DictionaryTree<string, string> groups, 
                                treatIndexLocal);
                            GroupBulkInsertByName(groups);
                            DataBulkInsert(datas, treatIndex);
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Thread.Sleep(30 * 60 * 1000);
                        }
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            BulkInsertDespesas();
            //BulkInsertViagens();
        }

        private static List<ViagensCsv> GetViagensCsv(int year)
        {
            var parameters = "{" + $"\"edtExercicio\":{year},\"edtProcesso\":\"%\",\"edtDestino\":\"%\"" + "}";
            var parametersEncoded = Encoding.UTF8.GetBytes(parameters);
            var parametersBase64 = Convert.ToBase64String(parametersEncoded);
            var url = "http://eportal.osasco.sp.gov.br/eportais-api/api/relatorio/gerarRelatorioParam/RelacaoDespesaViagem/CSV/" + parametersBase64;
            var request = new RestRequest(Method.GET);
            var response = new RestClient(url)
                .Execute(request);

            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.Content);
            }

            var viagens = new List<ViagensCsv>();

            using (var stream = new System.IO.MemoryStream(response.RawBytes))
            using (var rd = new System.IO.StreamReader(stream, Encoding.UTF7))
            {
                string row = null;

                while ((row = rd.ReadLine()) != null)
                {
                    if(row.StartsWith("Ano"))
                        continue;

                    var columns = row.Split(';');

                    try
                    {
                        //var tmpPeriodo = columns[3].Split(' ');
                        //var dtInicio = DateTime.Parse(tmpPeriodo[0]);
                        //var dtFim = DateTime.Parse(tmpPeriodo.Last());
                        var valor = decimal.Parse(columns[5], CultureInfo.InvariantCulture);
                        var viagem = new ViagensCsv()
                        {
                            Nro_Processo = int.Parse(columns[1]),
                            Destino = columns[2],
                            Valor = valor,
                            Motivo = columns[6],
                            Observacao = columns[7],
                            Nome = columns[8],
                            Cargo = columns[9]
                        };

                        viagens.Add(viagem);
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }
                }
            }

            return viagens;
        }

        private static List<ViagensXml> GetViagensXml(int year)
        {
            var parameters = "{" + $"\"edtExercicio\":{year},\"edtProcesso\":\"%\",\"edtDestino\":\"%\"" + "}";
            var parametersEncoded = Encoding.UTF8.GetBytes(parameters);
            var parametersBase64 = Convert.ToBase64String(parametersEncoded);
            var client = new RestClient("http://eportal.osasco.sp.gov.br/eportais-api/api/relatorio/gerarRelatorioXML/Relacao%20Despesa%20Viagem%20Osasco/" + parametersBase64);
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content);

            var viagens = new List<ViagensXml>();
            var xml = response.Content;
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(xml);

            var xpath = "RelacaoDespesaViagemOsasco/registro";
            var nodes = xmlDoc.SelectNodes(xpath);

            try
            {
                foreach (System.Xml.XmlNode childrenNode in nodes)
                {
                    var valor = childrenNode.SelectSingleNode("vl_medio_participante").InnerText;

                    if (valor.Length >= 30)
                        valor = valor.Substring(0, 30);

                    var viagem = new ViagensXml()
                    {
                        Nro_Processo = int.Parse(childrenNode.SelectSingleNode("nr_processo").InnerText),
                        Destino = childrenNode.SelectSingleNode("ds_destinoviagem").InnerText,
                        Motivo = childrenNode.SelectSingleNode("ds_motivo").InnerText,
                        Observacao = childrenNode.SelectSingleNode("ds_observacao").InnerText,
                        Orgao = childrenNode.SelectSingleNode("ds_orgao").InnerText,
                        Unidade = childrenNode.SelectSingleNode("ds_unidade").InnerText,
                        Valor = decimal.Parse(valor, CultureInfo.InvariantCulture),
                        Inicio = DateTime.Parse(childrenNode.SelectSingleNode("dt_inicio").InnerText),
                        Fim = DateTime.Parse(childrenNode.SelectSingleNode("dt_termino").InnerText)
                    };

                    viagens.Add(viagem);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return viagens;
        }

        private static void GetViagens(int year, out List<DataPrfOsasco> datas, out DictionaryTree<string, string> despesas, Func<string, string> treatIndex)
        {
            datas = new List<DataPrfOsasco>();
            despesas = new DictionaryTree<string, string>(s => treatIndex(s), "Viagens");

            var viagensCsv = GetViagensCsv(year);
            var viagensXml = GetViagensXml(year);

            var viagens = from x in viagensXml
                           join c in viagensCsv on new { x.Nro_Processo } equals new { c.Nro_Processo }
                           select new { x.Destino, x.Motivo, x.Observacao, x.Valor, x.Inicio, x.Fim, x.Orgao, x.Unidade, c.Nome, c.Cargo };
            
            if (viagensCsv.Count() != viagens.Count())
                throw new Exception("Missing data");

            foreach (var viagem in viagens)
            {
                var groupValor = despesas.AddIfNew(
                    viagem.Orgao,
                    viagem.Unidade,
                    viagem.Destino,
                    viagem.Motivo,
                    viagem.Observacao,
                    viagem.Cargo,
                    viagem.Nome,
                    "Valor");

                var dataValor = new DataPrfOsasco()
                {
                    Date = viagem.Inicio,
                    Group = groupValor,
                    Value = viagem.Valor
                };

                datas.Add(dataValor);

                var groupDias = despesas.AddIfNew(
                    "Prefeituras",
                    "Osasco",
                    "Viagens",
                    viagem.Orgao,
                    viagem.Unidade,
                    viagem.Destino,
                    viagem.Motivo,
                    viagem.Observacao,
                    viagem.Cargo,
                    viagem.Nome,
                    "Dias");

                var dataDias = new DataPrfOsasco()
                {
                    Date = viagem.Inicio,
                    Group = groupDias,
                    Value = viagem.Valor
                };

                datas.Add(dataDias);
            }
        }

        private static void GetDespesas(int year, int month, out List<DataPrfOsasco> datas, out DictionaryTree<string, string> despesas, Func<string, string> treatIndex)
        {
            datas = new List<DataPrfOsasco>();
            despesas = new DictionaryTree<string, string>(s => treatIndex(s), "Despesas");

            var parameters = "{" + $"\"edtExercicio\":{year},\"edtMes\":{month},\"edtCategoria\":\"%\"" + "}";
            var parametersEncoded = Encoding.UTF8.GetBytes(parameters);
            var parametersBase64 = Convert.ToBase64String(parametersEncoded);
            var client = new RestClient("http://eportal.osasco.sp.gov.br/eportais-api/api/relatorio/gerarRelatorioXML/Despesa%20paga%20por%20Fornecedor/" + parametersBase64);
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content);

            var xml = response.Content;
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(xml);

            var xpath = "DespesapagaporFornecedor/registro";
            var nodes = xmlDoc.SelectNodes(xpath);

            foreach (System.Xml.XmlNode childrenNode in nodes)
            {
                var year2 = int.Parse(childrenNode.SelectSingleNode("nr_ano").InnerText);
                var month2 = int.Parse(childrenNode.SelectSingleNode("nr_mes").InnerText);
                var date = new DateTime(year2, month2, 1);
                var valor = decimal.Parse(childrenNode.SelectSingleNode("vl_pagamento").InnerText, CultureInfo.InvariantCulture);
                var cpfCnpj = childrenNode.SelectSingleNode("cnpj").InnerText;
                var tpPessoa = "Indefinido";

                if (IsCpf(cpfCnpj))
                {
                    tpPessoa = "Pessoa";
                }
                else if (IsCnpj(cpfCnpj))
                {
                    tpPessoa = "Empresa";
                }

                var despesa = despesas.AddIfNew(
                    childrenNode.SelectSingleNode("ds_categoria").InnerText,
                    childrenNode.SelectSingleNode("ds_grupo").InnerText,
                    childrenNode.SelectSingleNode("ds_modalidade").InnerText,
                    childrenNode.SelectSingleNode("ds_despesa").InnerText,
                    childrenNode.SelectSingleNode("ds_orgao").InnerText,
                    childrenNode.SelectSingleNode("ds_unidade").InnerText,
                    tpPessoa,
                    childrenNode.SelectSingleNode("razao_social").InnerText);

                var data = new DataPrfOsasco()
                {
                    Date = date,
                    Group = despesa,
                    Value = valor
                };

                datas.Add(data);

                ////Console.WriteLine(childrenNode.SelectSingleNode("razao_social").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_processo").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_orgao").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_orgao").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_unidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_unidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("categoria_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_categoria").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("grupo_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_grupo").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_modalidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_modalidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_mes").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_ano").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("vl_pagamento").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cnpj").InnerText);
            }
        }

        private static void GroupBulkInsertByName(DictionaryTree<string, string> groups)
        {
            if (!groups.Any())
                return;

            var request = new RestRequest(Method.POST)
            {
                Timeout = int.MaxValue,
                RequestFormat = DataFormat.Json
            };
            var body = new { NewGroups = (GroupNameTree)groups , RootPath = rootPath };

            request.AddJsonBody(body);

            var url = "http://localhost:58994/odata/v4/groups/BulkInsertByName";
            var response = new RestClient(url)
                .Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.Content);
            }
        }

        private static void DataBulkInsert(List<DataPrfOsasco> datas, Func<string, string> treatIndex)
        {
            if (!datas.Any())
                return;

            var dataUriStr = "http://localhost:58994/odata/v4";
            var dataUri = new Uri(dataUriStr);
            var container = new Default.Container(dataUri)
            {
                Timeout = int.MaxValue
            };

            var groupsDbDictionary = container.Groups.ToDictionaryTree(g => treatIndex(g.Name), OsascoGroupId);
            var datas2 = datas
                .GroupBy(e => string.Join("/", e.Group.Key) + "/" + e.Date.ToString())
                .Select(eg =>
                    new Data.Models.DataDecimal()
                    {
                        CollectionDate = eg.First().Date,
                        GroupId = groupsDbDictionary[eg.First().Group.Key].Data.Id,
                        DecimalValue = eg.Sum(e => e.Value)
                    })
                .ToList<Data.Models.Data>();

            var bulkInsert = Default.ExtensionMethods.BulkInsert(container.Datas, datas2);
            bulkInsert.Execute();
        }
    }
}
