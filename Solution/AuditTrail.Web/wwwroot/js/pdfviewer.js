// PDF Viewer JavaScript
var PdfViewer = {
    config: {
        fileId: null,
        fileName: null,
        downloadUrl: null,
        viewerFrame: null
    },

    init: function(fileId, fileName, downloadUrl) {
        this.config.fileId = fileId;
        this.config.fileName = fileName;
        this.config.downloadUrl = downloadUrl;
        this.config.viewerFrame = document.getElementById('pdfViewer');
        
        this.initializeViewer();
        this.bindEvents();
        this.trackView();
    },

    initializeViewer: function() {
        var self = this;
        
        // Show loading state
        //this.showLoading();
        
        // Handle iframe load events
        if (this.config.viewerFrame) {
            this.config.viewerFrame.onload = function() {
                self.hideLoading();
                console.log('PDF Viewer loaded successfully');
            };
            
            this.config.viewerFrame.onerror = function() {
                self.showError('Error al cargar el documento PDF');
            };
        }
    },

    bindEvents: function() {
        var self = this;
        
        // Download button
        $('#btnDownloadPdf').on('click', function() {
            self.downloadPdf();
        });
        
        // Close button
        $('#btnClosePdf').on('click', function() {
            if (window.opener) {
                window.close();
            } else {
                window.history.back();
            }
        });
        
        // Keyboard shortcuts
        $(document).on('keydown', function(e) {
            // Ctrl+D for download
            if (e.ctrlKey && e.key === 'd') {
                e.preventDefault();
                self.downloadPdf();
            }
            // Escape to close
            if (e.key === 'Escape') {
                if (window.opener) {
                    window.close();
                } else {
                    window.history.back();
                }
            }
        });
    },

    trackView: function() {
        // View tracking is done server-side when the page loads
        console.log('PDF view tracked for file:', this.config.fileId);
    },

    downloadPdf: function() {
        // The download URL already includes tracking
        window.location.href = this.config.downloadUrl;
        
        // Show toast notification
        this.showToast('Descargando ' + this.config.fileName + '...', 'info');
    },



    hideLoading: function() {
        $('#pdfLoadingIndicator').fadeOut(300, function() {
            $(this).remove();
        });
    },

    showError: function(message) {
        this.hideLoading();
        var errorHtml = '<div class="pdf-error">' +
                       '<i class="bi bi-exclamation-triangle-fill"></i>' +
                       '<h4>Error al cargar el PDF</h4>' +
                       '<p>' + message + '</p>' +
                       '<button class="btn btn-primary" onclick="location.reload()">' +
                       '<i class="bi bi-arrow-clockwise"></i> Reintentar' +
                       '</button>' +
                       '</div>';
        $('#pdfViewerContainer').html(errorHtml);
    },

    showToast: function(message, type) {
        // Simple toast notification
        var toastClass = type === 'error' ? 'bg-danger' : type === 'success' ? 'bg-success' : 'bg-info';
        var toastHtml = '<div class="position-fixed bottom-0 end-0 p-3" style="z-index: 11">' +
                       '<div class="toast show ' + toastClass + ' text-white" role="alert">' +
                       '<div class="toast-body">' + message + '</div>' +
                       '</div>' +
                       '</div>';
        
        var $toast = $(toastHtml);
        $('body').append($toast);
        
        setTimeout(function() {
            $toast.fadeOut(300, function() {
                $(this).remove();
            });
        }, 3000);
    },

    // Utility function to format file size
    formatFileSize: function(bytes) {
        if (bytes === 0) return '0 Bytes';
        var k = 1024;
        var sizes = ['Bytes', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }
};