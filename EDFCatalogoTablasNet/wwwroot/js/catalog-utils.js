// Utilidades para el catálogo de tablas
window.catalogUtils = {
    // Descargar archivo
    downloadFile: function (fileName, mimeType, content) {
        const blob = new Blob([content], { type: mimeType });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    },

    // Copiar texto al portapapeles
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            // Fallback para navegadores que no soportan clipboard API
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            return true;
        }
    },

    // Mostrar modal de archivo con metadata
    showFileModalWithMetadata: function (fileData) {
        const modalHtml = `
            <div class="modal fade" id="fileModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="${this.getFileIcon(fileData.fileType)} me-2"></i>
                                ${fileData.fileName}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-12">
                                    ${this.getFilePreview(fileData.fileUrl, fileData.fileType)}
                                </div>
                            </div>
                            <hr>
                            <div class="row">
                                <div class="col-md-6">
                                    <strong>Tipo:</strong> ${fileData.fileType}
                                </div>
                                <div class="col-md-6">
                                    <strong>Tamaño:</strong> ${fileData.fileSize || 'N/A'}
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cerrar</button>
                            <a href="${fileData.fileUrl}" class="btn btn-primary" target="_blank">
                                <i class="fas fa-external-link-alt me-1"></i>Abrir en nueva pestaña
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remover modal existente si existe
        const existingModal = document.getElementById('fileModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Agregar nuevo modal
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Mostrar modal
        const modal = new bootstrap.Modal(document.getElementById('fileModal'));
        modal.show();

        // Limpiar modal cuando se cierre
        document.getElementById('fileModal').addEventListener('hidden.bs.modal', function () {
            this.remove();
        });
    },

    // Obtener icono según tipo de archivo
    getFileIcon: function (fileType) {
        const iconMap = {
            'image': 'fas fa-image',
            'video': 'fas fa-video',
            'audio': 'fas fa-music',
            'document': 'fas fa-file-pdf',
            'file': 'fas fa-file'
        };
        return iconMap[fileType] || 'fas fa-file';
    },

    // Obtener preview del archivo
    getFilePreview: function (fileUrl, fileType) {
        switch (fileType) {
            case 'image':
                return `<img src="${fileUrl}" class="img-fluid" alt="Preview">`;
            case 'video':
                return `<video class="w-100" controls><source src="${fileUrl}" type="video/mp4"></video>`;
            case 'audio':
                return `<audio class="w-100" controls><source src="${fileUrl}"></audio>`;
            case 'document':
                return `<iframe src="${fileUrl}" class="w-100" style="height: 400px;"></iframe>`;
            default:
                return `<div class="text-center p-4"><i class="fas fa-file fa-3x text-muted"></i><p class="mt-2">Vista previa no disponible</p></div>`;
        }
    }
};

// Funciones globales para compatibilidad con Blazor
window.downloadFile = window.catalogUtils.downloadFile;
window.copyToClipboard = window.catalogUtils.copyToClipboard;
window.showFileModalWithMetadata = window.catalogUtils.showFileModalWithMetadata;
