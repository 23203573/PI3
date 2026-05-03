# Fase 2: Implementação Completa de Checking/Checkout com Notificações

## Contexto
O modelo `Notificacao` foi estendido com suporte para avisos de checking/checkout com estados (Pendente, Realizado, Atrasado). A migration foi criada (commit `423ffe7`).

## O que falta implementar:

### 1. **Atualizar `NotificacoesController.cs`** (~400 linhas)
   - Estender `GerarNotificacoesAutomaticas()` para:
     - **Checking:** Criar notificações do tipo `CheckingPendente` no dia de entrada da reserva (DataEntrada)
     - **Checkout:** Criar notificações do tipo `CheckoutPendente` no dia de saída da reserva (DataSaida)
     - **Estados:** Se não confirmado até 24h após o dia, mudar para `CheckingAtrasado` ou `CheckoutAtrasado`
   
   - Criar ação `ConfirmarChecking(int notificacaoId)` que:
     - Marca notificação como `Status = StatusNotificacao.Realizado`
     - Seta `DataConfirmacao = DateTime.Now`
     - Retorna à listagem com mensagem de sucesso
   
   - Criar ação `ConfirmarCheckout(int notificacaoId)` com mesma lógica

### 2. **Atualizar `Views/Notificacoes/Index.cshtml`** (~100 linhas)
   - Adicionar botões V (✓) para notificações com `Tipo` de checking/checkout
   - Botão deve chamar `ConfirmarChecking` ou `ConfirmarCheckout` conforme tipo
   - Exibir mensagem diferente se notificação está `Realizado`, `Atrasado`, ou `Pendente`
   - Cores: Pendente = amarelo/warning, Realizado = verde/success, Atrasado = vermelho/danger

### 3. **Bloquear liberação de quarto se checkout pendente**
   - **Em `HomeController.cs` (Dashboard):**
     - Suíte só fica `Livre` se:
       - Reserva encerrada (Status = Encerrada)
       - **E** Não há notificação `CheckoutPendente` ativa para essa reserva
       - **E** Não há suíte em `EmManutencao`
       - **E** Não há suíte em `EmLimpeza`
   
   - **Em `PublicController.cs` (Área pública - ReservarSuite):**
     - Mesmo bloqueio na filtragem de suítes disponíveis
     - Adicionar validação: `AnyAsync(n => n.Tipo == TipoNotificacao.CheckoutPendente && n.ReservaId == ...)`

### 4. **Testes recomendados**
   - Dashboard: Verificar que quarto fica bloqueado com checkout pendente
   - Notificações: Clicar em botão V, ver mensagem de sucesso
   - Área pública: Tentar reservar quarto com checkout pendente (deve estar indisponível)
   - Automático: Passadas 24h sem confirmar, status deve mudar para "Atrasado"

## Arquivos a modificar:
1. `PI3_App/Controllers/NotificacoesController.cs`
2. `PI3_App/Views/Notificacoes/Index.cshtml`
3. `PI3_App/Controllers/HomeController.cs` (regra de disponibilidade)
4. `PI3_App/Controllers/PublicController.cs` (filtragem suítes livres)

## Migration já criada:
- `PI3_App/Migrations/20260503154312_AdicionarStatusEDataConfirmacaoNotificacao.cs`

## Próximas ações:
1. Aplicar migration ao banco: `dotnet ef database update`
2. Implementar lógica no controller
3. Atualizar view com botões
4. Testar todo o fluxo
5. Commit final e push
