namespace CSharp.Politica.PrfOsascoTask
{
    using RestSharp;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using Data;

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

    public class ExamPrfOsasco
    {
        public DateTime Date { get; set; }
        public DictionaryTree<string, string> Group { get; set; }
        public decimal Value { get; set; }
    }

    public class body
    {
        public GroupNameTree Groups { get; set; }
    }

    public class body2
    {
        public List<Data.Models.Exam> Exams { get; set; }
    }

    class Program
    {
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

        static void Main(string[] args)
        {
            Func<string, string> treatIndexLocal = treatIndex;
            var year = 2019;
            var month = 1;

            GetData(year, month, out List<ExamPrfOsasco> exams, out DictionaryTree<string, string> groups, treatIndexLocal);
            GroupBulkInsertByName(groups);
            ExamBulkInsert(exams, treatIndex);
        }

        private static void GetData(int year, int month, out List<ExamPrfOsasco> exams, out DictionaryTree<string, string> groups, Func<string, string> treatIndex)
        {
            exams = new List<ExamPrfOsasco>();
            groups = new DictionaryTree<string, string>(s => treatIndex(s), "Política");

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
                var valor = decimal.Parse(childrenNode.SelectSingleNode("vl_pagamento").InnerText);
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

                var group = groups.AddIfNew(
                    "Prefeituras",
                    "Osasco",
                    "Despesas",
                    childrenNode.SelectSingleNode("ds_categoria").InnerText,
                    childrenNode.SelectSingleNode("ds_grupo").InnerText,
                    childrenNode.SelectSingleNode("ds_modalidade").InnerText,
                    childrenNode.SelectSingleNode("ds_despesa").InnerText,
                    childrenNode.SelectSingleNode("ds_orgao").InnerText,
                    childrenNode.SelectSingleNode("ds_unidade").InnerText,
                    tpPessoa,
                    childrenNode.SelectSingleNode("razao_social").InnerText);

                var exam = new ExamPrfOsasco()
                {
                    Date = date,
                    Group = group,
                    Value = valor
                };

                exams.Add(exam);

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

            var body = new body() { Groups = (GroupNameTree)groups };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var client2 = new RestClient("http://localhost:58994/odata/v4/groups/BulkInsertByName");
            var request2 = new RestRequest(Method.POST);
            request2.RequestFormat = DataFormat.Json;
            request2.AddJsonBody(json);
            var response2 = client2.Execute(request2);

            if (response2.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response2.Content);
            }
        }

        private static void ExamBulkInsert(List<ExamPrfOsasco> exams, Func<string, string> treatIndex)
        {
            if (!exams.Any())
                return;

            var dataUriStr = "http://localhost:58994/odata/v4";
            var dataUri = new Uri(dataUriStr);
            var container = new Default.Container(dataUri);
            container.Timeout = int.MaxValue;

            var groupsDbDictionary = container.Groups.ToDictionaryTree(g => treatIndex(g.Name));
            var exams2 = exams
                .GroupBy(e => string.Join("/", e.Group.Key) + "/" + e.Date.ToString())
                .Select(eg =>
                    new Data.Models.ExamDecimal()
                    {
                        CollectionDate = eg.First().Date,
                        GroupId = groupsDbDictionary[eg.First().Group.Key].Data.Id,
                        DecimalValue = eg.Sum(e => e.Value)
                    })
                .ToList<Data.Models.Exam>();

            var bulkInsert = Default.ExtensionMethods.BulkInsert(container.Exams, exams2);
            bulkInsert.Execute();
        }
    }
}
