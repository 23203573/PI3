// Script para formatação de datas brasileiras (dd/mm/yyyy)

document.addEventListener('DOMContentLoaded', function() {
    // Aplicar máscara a todos os inputs com classe 'data-input'
    const dataInputs = document.querySelectorAll('.data-input');
    
    dataInputs.forEach(input => {
        // Máscara de data dd/mm/yyyy
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, ''); // Remove não-dígitos
            
            if (value.length >= 2) {
                value = value.substring(0, 2) + '/' + value.substring(2);
            }
            if (value.length >= 5) {
                value = value.substring(0, 5) + '/' + value.substring(5, 9);
            }
            
            e.target.value = value;
        });

        // Validação de data
        input.addEventListener('blur', function(e) {
            const value = e.target.value;
            if (value && !isValidDate(value)) {
                e.target.classList.add('is-invalid');
                // Adicionar mensagem de erro se não existir
                let errorMsg = e.target.parentNode.querySelector('.invalid-feedback');
                if (!errorMsg) {
                    errorMsg = document.createElement('div');
                    errorMsg.className = 'invalid-feedback';
                    errorMsg.textContent = 'Data inválida. Use o formato dd/mm/aaaa';
                    e.target.parentNode.appendChild(errorMsg);
                }
            } else {
                e.target.classList.remove('is-invalid');
                const errorMsg = e.target.parentNode.querySelector('.invalid-feedback');
                if (errorMsg) {
                    errorMsg.remove();
                }
            }
        });
    });
});

function isValidDate(dateString) {
    const regex = /^(\d{2})\/(\d{2})\/(\d{4})$/;
    const match = dateString.match(regex);
    
    if (!match) return false;
    
    const day = parseInt(match[1], 10);
    const month = parseInt(match[2], 10);
    const year = parseInt(match[3], 10);
    
    if (month < 1 || month > 12) return false;
    
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && 
           date.getMonth() === month - 1 && 
           date.getDate() === day;
}