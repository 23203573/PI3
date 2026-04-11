using PensionatoApp.Models;
using System.Text;

namespace PensionatoApp.Services
{
    public class ContratoService
    {
        public string GerarContratoHtml(Reserva reserva)
        {
            var html = new StringBuilder();
            
            html.AppendLine($@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Contrato de Locação - Suíte {reserva.Suite.Numero}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .section {{ margin: 20px 0; }}
        .signature {{ margin-top: 50px; display: flex; justify-content: space-between; }}
        .signature div {{ text-align: center; width: 45%; }}
        .signature-line {{ border-top: 1px solid #000; margin-top: 50px; padding-top: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>CONTRATO DE LOCAÇÃO DE SUÍTE ESTUDANTIL</h2>
        <p><strong>Pensionato Estudantil XYZ</strong></p>
    </div>

    <div class='section'>
        <h3>IDENTIFICAÇÃO DAS PARTES</h3>
        
        <p><strong>LOCADOR:</strong><br>
        Pensionato da Penha<br>
        CNPJ: 00.000.000/0001-00<br>
        Endereço: Rua Exemplo, 123 - Cidade/UF</p>
        
        <p><strong>LOCATÁRIO:</strong><br>
        Nome: {reserva.Hospede.NomeCompleto}<br>
        Documento: {(reserva.Hospede.EhBrasileiro ? (!string.IsNullOrEmpty(reserva.Hospede.CPF) ? reserva.Hospede.CPF : reserva.Hospede.RG) : reserva.Hospede.NumeroDocumentoEstrangeiro)}<br>
        Telefone: {reserva.Hospede.Telefone}<br>
        E-mail: {reserva.Hospede.Email}<br>
        Endereço: {reserva.Hospede.Endereco}</p>
    </div>

    <div class='section'>
        <h3>OBJETO DO CONTRATO</h3>
        
        <table>
            <tr>
                <th>Suíte</th>
                <th>Tipo de Cama</th>
                <th>Valor Mensal</th>
                <th>Data Entrada</th>
                <th>Data Saída</th>
            </tr>
            <tr>
                <td>Suíte {reserva.Suite.Numero}</td>
                <td>{reserva.Suite.TipoCama}</td>
                <td>R$ {reserva.ValorMensalTotal:N2}</td>
                <td>{reserva.DataEntrada:dd/MM/yyyy}</td>
                <td>{reserva.DataSaida:dd/MM/yyyy}</td>
            </tr>
        </table>
    </div>

    <div class='section'>
        <h3>SERVIÇOS INCLUSOS E ADICIONAIS</h3>
        
        <p><strong>Inclusos no valor:</strong></p>
        <ul>
            <li>Uso da suíte mobiliada</li>
            <li>Limpeza semanal da suíte e áreas comuns</li>
            <li>Uso das áreas comuns (cozinhas, salas de estudo, lavanderia, etc.)</li>
            <li>Wi-Fi</li>
            <li>Ventilador, geladeira, mesa de estudos</li>
        </ul>
        
        <p><strong>Serviços adicionais contratados:</strong></p>
        <ul>");

            if (reserva.TemGaragem)
            {
                html.AppendLine($"<li>Garagem: R$ {reserva.PrecoGaragem:N2}/mês</li>");
            }
            
            if (reserva.TemArCondicionado)
            {
                html.AppendLine($"<li>Ar-condicionado: R$ {reserva.PrecoArCondicionado:N2}/mês + energia</li>");
            }

            html.AppendLine($@"
        </ul>
    </div>

    <div class='section'>
        <h3>REGRAS INTERNAS</h3>
        
        <ul>
            <li><strong>Silêncio:</strong> Das 23h às 7h</li>
            <li><strong>Visitas:</strong> Permitidas em áreas comuns das 7h às 22h</li>
            <li><strong>Proibições:</strong> Fumar em áreas internas</li>
            <li><strong>Áreas comuns:</strong> Livre acesso a cozinhas, salas de TV e estudo, lavanderia, jardim</li>
        </ul>
    </div>

    <div class='section'>
        <h3>CONDIÇÕES FINANCEIRAS</h3>
        
        <table>
            <tr>
                <th>Item</th>
                <th>Valor</th>
            </tr>
            <tr>
                <td>Valor mensal total</td>
                <td>R$ {reserva.ValorMensalTotal:N2}</td>
            </tr>
            <tr>
                <td>Valor adiantado pago</td>
                <td>R$ {reserva.ValorAdiantado:N2}</td>
            </tr>
            <tr>
                <td>Valor caução</td>
                <td>R$ {reserva.ValorCaucao:N2}</td>
            </tr>
        </table>
        
        <p><strong>Forma de pagamento:</strong> Mensal, até o dia do vencimento<br>
        <strong>Reajuste:</strong> Anual, conforme legislação vigente</p>
    </div>

    <div class='section'>
        <h3>CLÁUSULAS ESPECIAIS</h3>
        
        <ol>
            <li>O presente contrato terá vigência de {reserva.DataEntrada:dd/MM/yyyy} até {reserva.DataSaida:dd/MM/yyyy}.</li>
            <li>A rescisão antecipada por parte do locatário implicará no pagamento de multa equivalente a 3 aluguéis.</li>
            <li>Problemas elétricos e hidráulicos são de responsabilidade do locador.</li>
            <li>Troca de resistência de chuveiro é de responsabilidade do locatário.</li>
            <li>O valor da caução será devolvido com juros de poupança ao final do contrato, descontadas eventuais pendências.</li>
        </ol>
    </div>

    <div class='section'>
        <p><strong>Data do contrato:</strong> {DateTime.Now:dd/MM/yyyy}</p>
        
        {(string.IsNullOrEmpty(reserva.Observacoes) ? "" : $"<p><strong>Observações:</strong> {reserva.Observacoes}</p>")}
    </div>

    <div class='signature'>
        <div>
            <div class='signature-line'>
                <strong>LOCADOR</strong><br>
                Pensionato da Penha
            </div>
        </div>
        <div>
            <div class='signature-line'>
                <strong>LOCATÁRIO</strong><br>
                {reserva.Hospede.NomeCompleto}
            </div>
        </div>
    </div>
</body>
</html>");

            return html.ToString();
        }

        public string GerarReciboHtml(Pagamento pagamento)
        {
            return $@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 20px; }}
        .content {{ margin: 20px 0; }}
        .footer {{ margin-top: 30px; text-align: right; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>RECIBO DE PAGAMENTO</h2>
        <p><strong>Pensionato da Penha</strong></p>
    </div>

    <div class='content'>
        <p><strong>Recibo Nº:</strong> {pagamento.Id:D6}</p>
        <p><strong>Data:</strong> {pagamento.DataPagamento?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy")}</p>
        
        <hr/>
        
        <p><strong>Recebemos de:</strong> {pagamento.Reserva.Hospede.NomeCompleto}</p>
        <p><strong>Referente a:</strong> {pagamento.Descricao}</p>
        <p><strong>Suíte:</strong> {pagamento.Reserva.Suite.Numero}</p>
        <p><strong>Período:</strong> {pagamento.DataVencimento:MM/yyyy}</p>
        
        <hr/>
        
        <p><strong>Valor recebido:</strong> R$ {pagamento.ValorPago:N2}</p>
        <p><strong>Forma de pagamento:</strong> {pagamento.FormaPagamento}</p>
        
        {(string.IsNullOrEmpty(pagamento.Observacoes) ? "" : $"<p><strong>Observações:</strong> {pagamento.Observacoes}</p>")}
    </div>

    <div class='footer'>
        <p>_________________________________<br>
        Assinatura do Responsável</p>
    </div>
</body>
</html>";
        }
    }
}