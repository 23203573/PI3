// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Funcionalidade de Acessibilidade - Controle de Zoom
document.addEventListener('DOMContentLoaded', function() {
    // Configuração inicial
    let currentZoom = parseFloat(localStorage.getItem('fontZoom')) || 1.0;
    const minZoom = 0.8;
    const maxZoom = 2.0;
    const zoomStep = 0.1;
    
    // Aplicar zoom salvo
    applyZoom(currentZoom);
    updateZoomDisplay();
    
    // Funções de controle de zoom
    window.increaseFont = function() {
        if (currentZoom < maxZoom) {
            currentZoom = Math.min(currentZoom + zoomStep, maxZoom);
            applyZoom(currentZoom);
            saveZoom();
            updateZoomDisplay();
        }
    };
    
    window.decreaseFont = function() {
        if (currentZoom > minZoom) {
            currentZoom = Math.max(currentZoom - zoomStep, minZoom);
            applyZoom(currentZoom);
            saveZoom();
            updateZoomDisplay();
        }
    };
    
    window.resetFont = function() {
        currentZoom = 1.0;
        applyZoom(currentZoom);
        saveZoom();
        updateZoomDisplay();
    };
    
    function applyZoom(zoom) {
        document.documentElement.style.fontSize = (zoom * 16) + 'px';
        
        // Ajustar alguns elementos específicos que podem precisar de ajustes proporcionais
        const style = document.createElement('style');
        style.id = 'accessibility-zoom-style';
        
        // Remove estilo anterior se existir
        const existingStyle = document.getElementById('accessibility-zoom-style');
        if (existingStyle) {
            existingStyle.remove();
        }
        
        style.innerHTML = `
            .accessibility-zoom-content {
                font-size: ${zoom}rem !important;
            }
            .card {
                font-size: ${zoom}rem !important;
            }
            .btn {
                font-size: ${zoom * 0.875}rem !important;
            }
            .navbar-brand {
                font-size: ${zoom * 1.25}rem !important;
            }
        `;
        
        document.head.appendChild(style);
    }
    
    function saveZoom() {
        localStorage.setItem('fontZoom', currentZoom.toString());
    }
    
    function updateZoomDisplay() {
        const zoomDisplay = document.getElementById('zoom-percentage');
        if (zoomDisplay) {
            zoomDisplay.textContent = Math.round(currentZoom * 100) + '%';
        }
        
        // Atualizar estado dos botões
        const increaseBtn = document.getElementById('increase-font-btn');
        const decreaseBtn = document.getElementById('decrease-font-btn');
        
        if (increaseBtn) {
            increaseBtn.disabled = currentZoom >= maxZoom;
        }
        
        if (decreaseBtn) {
            decreaseBtn.disabled = currentZoom <= minZoom;
        }
    }
});

// Atalhos de teclado para acessibilidade
document.addEventListener('keydown', function(e) {
    // Ctrl + Plus (aumentar fonte)
    if (e.ctrlKey && (e.key === '+' || e.key === '=')) {
        e.preventDefault();
        window.increaseFont();
    }
    
    // Ctrl + Minus (diminuir fonte)
    if (e.ctrlKey && e.key === '-') {
        e.preventDefault();
        window.decreaseFont();
    }
    
    // Ctrl + 0 (resetar fonte)
    if (e.ctrlKey && e.key === '0') {
        e.preventDefault();
        window.resetFont();
    }
});