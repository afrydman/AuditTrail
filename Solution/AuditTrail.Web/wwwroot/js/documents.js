// Documents Management JavaScript
// Version: 1.0.0

let currentView = 'list';
let currentFolderId = null;
let currentFolderName = null;
let currentFolderPath = null;
let treeData = [];
let originalTreeData = []; // Keep original data for filtering
let filterTimeout = null;

$(document).ready(function() {
    initializeDocumentExplorer();
    loadTreeData();
    initializeTreeFilter();
});

function initializeDocumentExplorer() {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize select all checkbox
    $('#selectAllFiles').on('change', function() {
        $('.file-checkbox').prop('checked', this.checked);
    });
}

function loadTreeData() {
    console.log('Loading tree data...');
    $.get(getActionUrl('GetTreeData', 'Documents'))
        .done(function(data) {
            console.log('Tree data loaded successfully. Data:', data);
            console.log('Data type:', typeof data, 'Length:', data.length);
            treeData = data;
            originalTreeData = JSON.parse(JSON.stringify(data)); // Deep copy for filtering
            renderTree(data);
            updateFilterResults();
        })
        .fail(function(xhr, status, error) {
            console.error('Failed to load tree data. Status:', status, 'Error:', error);
            console.error('Response:', xhr.responseText);
            showToast('error', 'Error al cargar la estructura de carpetas');
            $('#documentTree').html('<div class="alert alert-danger">Error al cargar los datos</div>');
        });
}

// Make loadTreeData available globally
window.loadTreeData = loadTreeData;
window.refreshTreeData = function() {
    console.log('Refreshing tree data after file upload...');
    
    // Clear cached data
    treeData = [];
    originalTreeData = [];
    
    // Reload tree data
    loadTreeData();
    
    // If we're currently viewing a folder, refresh its contents too
    if (currentFolderId) {
        loadFolderContents(currentFolderId, currentFolderName);
    }
};

function renderTree(nodes, parentElement = '#documentTree') {
    if (!nodes || nodes.length === 0) {
        $(parentElement).html('<div class="text-muted p-3">No hay carpetas disponibles</div>');
        return;
    }

    let html = '<ul class="tree-list">';
    
    for (let node of nodes) {
        const hasChildren = node.children && node.children.length > 0;
        const expandIcon = hasChildren ? 'bi-chevron-right' : '';
        const nodeIcon = node.isCategory ? 'bi-folder' : node.icon;
        
        // Simplified rendering - no complex metadata for now
        html += `
            <li class="tree-node" data-node-id="${node.id}">
                <div class="tree-node-content" onclick="toggleNode('${node.id}')">
                    ${hasChildren ? `<i class="expand-icon bi ${expandIcon}"></i>` : '<span class="expand-spacer"></span>'}
                    <i class="node-icon bi ${nodeIcon} ${node.isCategory ? 'text-warning' : 'text-primary'}"></i>
                    <span class="node-title">${node.title}</span>
                    ${!node.isCategory && node.version ? `<span class="badge bg-info ms-1">v${node.version}</span>` : ''}
                </div>
                ${hasChildren ? `<ul class="tree-children" style="display: none;"></ul>` : ''}
            </li>
        `;
    }
    
    html += '</ul>';
    
    if (parentElement === '#documentTree') {
        $(parentElement).html(html);
    } else {
        $(parentElement).append(html);
    }
}

function toggleNode(nodeId) {
    const node = findNodeById(treeData, nodeId);
    if (!node) return;

    const treeNode = $(`.tree-node[data-node-id="${nodeId}"]`);
    const expandIcon = treeNode.find('.expand-icon');
    const childrenContainer = treeNode.find('.tree-children');

    if (node.isCategory) {
        // Handle folder click
        if (childrenContainer.length > 0) {
            const isExpanded = childrenContainer.is(':visible');
            
            if (isExpanded) {
                // Collapse
                childrenContainer.slideUp();
                expandIcon.removeClass('bi-chevron-down').addClass('bi-chevron-right');
            } else {
                // Expand
                if (childrenContainer.is(':empty') && node.children) {
                    renderTree(node.children, childrenContainer);
                }
                childrenContainer.slideDown();
                expandIcon.removeClass('bi-chevron-right').addClass('bi-chevron-down');
            }
        }
        
        // Load folder contents in the main area - remove HTML tags from title
        const cleanTitle = stripHtml(node.title);
        loadFolderContents(node.categoryId, cleanTitle);
    } else {
        // Handle file click - remove HTML tags from title
        const cleanTitle = stripHtml(node.title);
        loadFileDetails(node.fileId, cleanTitle);
    }
}

function stripHtml(html) {
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;
    return tempDiv.textContent || tempDiv.innerText || '';
}

function loadFileDetails(fileId, fileName) {
    // Show file details in the right panel
    $('#welcomeMessage, #folderContents').addClass('d-none');
    $('#fileDetails').removeClass('d-none');
    $('#fileDetailsTitle').text('Detalles del Archivo');
    
    // Find the file node to get more details
    const node = findNodeById(treeData, `file_${fileId}`);
    const isPdf = fileName.toLowerCase().endsWith('.pdf');
    const fileIcon = node ? node.icon : 'bi-file-earmark';
    const fileSize = node && node.fileSize ? formatFileSize(node.fileSize) : 'Desconocido';
    const createdDate = node && node.createdDate ? new Date(node.createdDate).toLocaleString('es-ES') : 'Desconocido';
    const fileExtension = node ? node.fileExtension : getFileExtension(fileName);
    
    // Get file type display name
    const fileType = getFileTypeName(fileExtension);
    
    // Extract version info if available
    const versionInfo = node ? {
        version: node.version || 1,
        uploadedBy: node.uploadedBy || 'Desconocido',
        originalUploader: node.originalUploader || node.uploadedBy || 'Desconocido'
    } : { version: 1, uploadedBy: 'Desconocido', originalUploader: 'Desconocido' };

    $('#fileDetailsContent').html(`
        <div class="file-details-container">
            <!-- File Header -->
            <div class="file-header text-center mb-4">
                <div class="file-icon-large mb-2">
                    <i class="bi ${fileIcon} ${isPdf ? 'text-danger' : 'text-primary'}" style="font-size: 3rem;"></i>
                </div>
                <h5 class="mb-1">${fileName}</h5>
                <div class="mb-2">
                    <span class="badge bg-secondary me-1">${fileType}</span>
                    ${versionInfo.version > 1 ? `<span class="badge bg-info">v${versionInfo.version}</span>` : `<span class="badge bg-secondary">v${versionInfo.version}</span>`}
                </div>
            </div>
            
            <!-- Two Column Layout -->
            <div class="row">
                <!-- Left Column: Actions -->
                <div class="col-md-5">
                    <div class="file-actions-section">
                        <h6 class="text-primary mb-3 border-bottom pb-2">
                            <i class="bi bi-lightning me-2"></i>Acciones
                        </h6>
                        <div class="d-grid gap-2">
                            ${isPdf ? `
                            <button class="btn btn-success" onclick="viewPdf('${fileId}')">
                                <i class="bi bi-eye me-2"></i>Ver PDF
                            </button>
                            ` : ''}
                            <button class="btn btn-primary" onclick="downloadFile('${fileId}')">
                                <i class="bi bi-download me-2"></i>Descargar
                            </button>
                            <button class="btn btn-outline-info" onclick="showFileAuditTrail('${fileId}', '${fileName.replace(/'/g, "\\'")}')">
                                <i class="bi bi-clock-history me-2"></i>Historial
                            </button>
                            <button class="btn btn-outline-danger" onclick="deleteFile('${fileId}')">
                                <i class="bi bi-trash me-2"></i>Eliminar
                            </button>
                        </div>
                    </div>
                </div>
                
                <!-- Right Column: Metadata -->
                <div class="col-md-7">
                    <div class="file-metadata-section">
                        <h6 class="text-primary mb-3 border-bottom pb-2">
                            <i class="bi bi-info-circle me-2"></i>Información
                        </h6>
                        <table class="table table-sm table-borderless">
                            <tbody>
                                <tr>
                                    <td class="text-muted fw-medium" style="width: 45%;">Tipo:</td>
                                    <td><strong>${fileType}</strong></td>
                                </tr>
                                <tr>
                                    <td class="text-muted fw-medium">Tamaño:</td>
                                    <td><strong>${fileSize}</strong></td>
                                </tr>
                                <tr>
                                    <td class="text-muted fw-medium">Versión:</td>
                                    <td><strong>v${versionInfo.version}</strong></td>
                                </tr>
                                <tr>
                                    <td class="text-muted fw-medium">Subido por:</td>
                                    <td><strong>${versionInfo.uploadedBy}</strong></td>
                                </tr>
                                ${versionInfo.uploadedBy !== versionInfo.originalUploader ? `
                                <tr>
                                    <td class="text-muted fw-medium">Autor original:</td>
                                    <td><strong>${versionInfo.originalUploader}</strong></td>
                                </tr>
                                ` : ''}
                                <tr>
                                    <td class="text-muted fw-medium">Fecha:</td>
                                    <td><strong>${createdDate}</strong></td>
                                </tr>
                                <tr>
                                    <td class="text-muted fw-medium">Extensión:</td>
                                    <td><code>${fileExtension}</code></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
            
            <!-- Additional Options -->
            <div class="mt-4 pt-3 border-top">
                <small class="text-muted">
                    <i class="bi bi-shield-check me-1"></i>
                    Todas las acciones son registradas en el sistema de auditoría
                </small>
            </div>
        </div>
    `);
    
    // Initialize tooltips for the new content
    $('[data-bs-toggle="tooltip"]').tooltip();
}

function getFileExtension(fileName) {
    const lastDot = fileName.lastIndexOf('.');
    return lastDot > -1 ? fileName.substring(lastDot) : '';
}

function getFileTypeName(extension) {
    const ext = extension?.toLowerCase();
    switch(ext) {
        case '.pdf': return 'PDF';
        case '.doc':
        case '.docx': return 'Word';
        case '.xls':
        case '.xlsx': return 'Excel';
        case '.ppt':
        case '.pptx': return 'PowerPoint';
        case '.jpg':
        case '.jpeg':
        case '.png':
        case '.gif':
        case '.bmp': return 'Imagen';
        case '.mp4':
        case '.avi':
        case '.mov':
        case '.wmv': return 'Video';
        case '.mp3':
        case '.wav':
        case '.flac': return 'Audio';
        case '.zip':
        case '.rar':
        case '.7z': return 'Archivo Comprimido';
        case '.txt': return 'Texto';
        default: return 'Archivo';
    }
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}

function findNodeById(nodes, id) {
    for (let node of nodes) {
        if (node.id === id) {
            return node;
        }
        if (node.children) {
            const found = findNodeById(node.children, id);
            if (found) return found;
        }
    }
    return null;
}

function loadFolderContents(folderId, folderName) {
    currentFolderId = folderId;
    currentFolderName = folderName;
    
    // Build full path for the current folder
    currentFolderPath = buildFolderPath(folderId);
    
    // Update breadcrumb
    updateBreadcrumb(folderName);
    
    // Show loading state
    $('#welcomeMessage, #fileDetails').addClass('d-none');
    $('#loadingContent').removeClass('d-none');
    
    // Load folder contents from API
    $.get(getActionUrl('GetFolderContents', 'Documents'), { categoryId: folderId })
        .done(function(files) {
            $('#loadingContent').addClass('d-none');
            $('#folderContents').removeClass('d-none');
            
            renderFileList(files);
        })
        .fail(function() {
            $('#loadingContent').addClass('d-none');
            showToast('error', 'Error al cargar el contenido de la carpeta');
            renderFileList([]);
        });
}

function buildFolderPath(folderId) {
    if (!folderId || !originalTreeData.length) {
        return '/';
    }
    
    const path = [];
    let currentNode = findNodeById(originalTreeData, `cat_${folderId}`);
    
    // Build path by traversing up the tree
    while (currentNode) {
        if (currentNode.isCategory) {
            path.unshift(currentNode.title);
        }
        
        // Find parent node
        const parentId = getParentId(originalTreeData, currentNode.id);
        if (parentId) {
            currentNode = findNodeById(originalTreeData, parentId);
        } else {
            break;
        }
    }
    
    return path.length > 0 ? '/' + path.join('/') + '/' : '/';
}

function getParentId(nodes, nodeId) {
    for (let node of nodes) {
        if (node.children) {
            for (let child of node.children) {
                if (child.id === nodeId) {
                    return node.id;
                }
            }
            // Recursively search in children
            const parentId = getParentId(node.children, nodeId);
            if (parentId) {
                return parentId;
            }
        }
    }
    return null;
}

function renderFileList(items) {
    const tbody = $('#fileListBody');
    tbody.empty();
    
    if (items.length === 0) {
        tbody.html('<tr><td colspan="6" class="text-center text-muted py-4">Esta carpeta no tiene contenido o no tienes acceso para verlo</td></tr>');
        return;
    }
    
    for (let item of items) {
        const isFolder = item.isFolder === true;
        const clickHandler = isFolder 
            ? `onclick="loadFolderContents(${item.categoryId}, '${item.name}')"`
            : `onclick="loadFileDetails('${item.id}', '${item.name}')"`;
        const rowClass = isFolder ? 'table-row-folder' : 'table-row-file';
        const iconClass = isFolder ? 'text-warning' : 'text-primary';
        
        tbody.append(`
            <tr data-file-path="${item.filePath || ''}" class="${rowClass}" ${clickHandler} style="cursor: pointer;">
                <td>
                    <div class="form-check">
                        <input class="form-check-input file-checkbox" type="checkbox" value="${item.id}" ${isFolder ? 'disabled' : ''}>
                    </div>
                </td>
                <td>
                    <div class="d-flex align-items-center">
                        <i class="bi ${item.icon} ${iconClass} me-2 fs-5"></i>
                        <span>${item.name}</span>
                        ${!isFolder && item.version && item.version > 1 ? `
                        <span class="badge bg-info ms-2" data-bs-toggle="tooltip" title="Versión ${item.version} - Última versión">v${item.version}</span>
                        ` : ''}
                    </div>
                </td>
                <td>${item.type}</td>
                <td>${item.size}</td>
                <td>${item.modified}</td>
                <td class="text-end">
                    ${!isFolder ? `
                    <div class="btn-group btn-group-sm">
                        ${item.name.toLowerCase().endsWith('.pdf') ? `
                        <button class="btn btn-outline-success" onclick="viewPdf('${item.id}')" data-bs-toggle="tooltip" title="Ver PDF">
                            <i class="bi bi-file-pdf"></i>
                        </button>
                        ` : ''}
                        <button class="btn btn-outline-primary" onclick="downloadFile('${item.id}')" data-bs-toggle="tooltip" title="Descargar">
                            <i class="bi bi-download"></i>
                        </button>
                        <button class="btn btn-outline-secondary" onclick="viewFile('${item.id}')" data-bs-toggle="tooltip" title="Ver">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button class="btn btn-outline-info" onclick="showFileAuditTrail('${item.id}', '${item.name}')" data-bs-toggle="tooltip" title="Ver Auditoría">
                            <i class="bi bi-clock-history"></i>
                        </button>
                        <button class="btn btn-outline-danger" onclick="deleteFile('${item.id}')" data-bs-toggle="tooltip" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                    ` : `
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-secondary" onclick="loadFolderContents(${item.categoryId}, '${item.name}')" data-bs-toggle="tooltip" title="Abrir">
                            <i class="bi bi-folder-open"></i>
                        </button>
                    </div>
                    `}
                </td>
            </tr>
        `);
    }
}

function updateBreadcrumb(currentFolder) {
    // Build the full path from root to current folder
    const breadcrumbPath = buildBreadcrumbPath(currentFolderId);
    let breadcrumbHtml = `
        <li class="breadcrumb-item">
            <i class="bi bi-house-door"></i>
            <span>Raíz</span>
        </li>
    `;
    
    // Add each folder in the path
    for (let i = 0; i < breadcrumbPath.length; i++) {
        const pathItem = breadcrumbPath[i];
        const isLast = i === breadcrumbPath.length - 1;
        
        if (isLast) {
            breadcrumbHtml += `
                <li class="breadcrumb-item active">
                    <i class="bi bi-folder"></i>
                    ${pathItem.name}
                </li>
            `;
        } else {
            breadcrumbHtml += `
                <li class="breadcrumb-item">
                    <a href="#" onclick="loadFolderContents(${pathItem.id}, '${pathItem.name}')" class="text-decoration-none">
                        <i class="bi bi-folder"></i>
                        ${pathItem.name}
                    </a>
                </li>
            `;
        }
    }
    
    $('#fileBreadcrumb').html(breadcrumbHtml);
}

function buildBreadcrumbPath(folderId) {
    if (!folderId || !originalTreeData.length) {
        return [];
    }
    
    const path = [];
    let currentNode = findNodeById(originalTreeData, `cat_${folderId}`);
    
    // Build path by traversing up the tree
    while (currentNode) {
        if (currentNode.isCategory) {
            path.unshift({
                id: currentNode.categoryId,
                name: stripHtml(currentNode.title)
            });
        }
        
        // Find parent node
        const parentId = getParentId(originalTreeData, currentNode.id);
        if (parentId) {
            currentNode = findNodeById(originalTreeData, parentId);
        } else {
            break;
        }
    }
    
    return path;
}

function toggleView(viewType) {
    currentView = viewType;
    
    if (viewType === 'grid') {
        $('#listView').addClass('d-none');
        $('#gridView').removeClass('d-none');
        $('#gridViewBtn').addClass('active');
        $('#listViewBtn').removeClass('active');
    } else {
        $('#gridView').addClass('d-none');
        $('#listView').removeClass('d-none');
        $('#listViewBtn').addClass('active');
        $('#gridViewBtn').removeClass('active');
    }
}

function openUploadModal() {
    $.get(getActionUrl('UploadFile', 'Documents'))
        .done(function(response) {
            $('#modalContainer').html(response);
            
            // Set current folder if one is selected
            if (currentFolderId) {
                $('#selectedCategoryId').val(currentFolderId);
                const displayPath = currentFolderPath || `/${currentFolderName}/` || '/Raíz/';
                $('#selectedFolderName').val(displayPath);
            }
            
            var modal = new bootstrap.Modal(document.getElementById('uploadFileModal'));
            modal.show();
        })
        .fail(function() {
            showToast('error', 'Error al cargar el formulario de subida');
        });
}

function createNewFolder() {
    $.get(getActionUrl('CreateFolder', 'Documents'))
        .done(function(response) {
            $('#modalContainer').html(response);
            
            // Set current folder as parent if one is selected
            if (currentFolderId) {
                $('#parentCategoryId').val(currentFolderId);
                const displayPath = currentFolderPath || `/${currentFolderName}/` || '/Raíz/';
                $('#parentFolderName').val(displayPath);
                updateFolderPathPreview();
            }
            
            var modal = new bootstrap.Modal(document.getElementById('createFolderModal'));
            modal.show();
        })
        .fail(function() {
            showToast('error', 'Error al cargar el formulario de nueva carpeta');
        });
}

function downloadFile(fileId) {
    // Download file using the file ID - use query parameter format
    window.open(`${getActionUrl('DownloadFile', 'Documents')}?id=${fileId}`, '_blank');
}

function viewFile(fileId) {
    // For now, just download the file
    downloadFile(fileId);
}

function viewPdf(fileId) {
    // Open PDF viewer in a new tab
    window.open(`${getActionUrl('ViewPdf', 'Documents')}/${fileId}`, '_blank');
}

function editFile(fileId) {
    showToast('info', 'Función de edición en desarrollo');
}

function deleteFile(fileId) {
    if (!confirm('¿Estás seguro de que deseas eliminar este archivo?')) {
        return;
    }

    $.post(getActionUrl('DeleteFile', 'Documents'), {
        fileId: fileId,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
    .done(function(response) {
        if (response.success) {
            showToast('success', response.message);
            
            // Refresh current folder contents
            if (currentFolderId) {
                loadFolderContents(currentFolderId, 'Carpeta actual');
            }
            
            // Refresh tree
            loadTreeData();
        } else {
            showToast('error', response.message);
        }
    })
    .fail(function() {
        showToast('error', 'Error al eliminar el archivo');
    });
}

function showToast(type, message) {
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    if (!$('.toast-container').length) {
        $('body').append('<div class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
    }
    
    const $toast = $(toastHtml);
    $('.toast-container').append($toast);
    const toast = new bootstrap.Toast($toast[0]);
    toast.show();
    
    $toast.on('hidden.bs.toast', function() {
        $(this).remove();
    });
}

// ============================================
// TREE FILTER FUNCTIONALITY
// ============================================

function initializeTreeFilter() {
    const filterInput = $('#treeFilter');
    
    // Real-time filtering with debounce
    filterInput.on('input', function() {
        clearTimeout(filterTimeout);
        filterTimeout = setTimeout(function() {
            filterTree(filterInput.val().trim());
        }, 300);
    });

    // Clear filter on Escape key
    filterInput.on('keydown', function(e) {
        if (e.key === 'Escape') {
            clearTreeFilter();
        }
    });
}

function filterTree(filterText) {
    if (!filterText) {
        // Show all nodes if no filter
        treeData = JSON.parse(JSON.stringify(originalTreeData));
        renderTree(treeData);
        updateFilterResults();
        return;
    }

    const filteredData = filterTreeNodes(originalTreeData, filterText.toLowerCase());
    treeData = filteredData;
    renderTree(filteredData);
    updateFilterResults(filterText);
    
    // Auto-expand all nodes when filtering
    if (filteredData.length > 0) {
        setTimeout(() => expandAllNodes(), 100);
    }
}

function filterTreeNodes(nodes, filterText) {
    const filtered = [];
    
    for (const node of nodes) {
        const nodeMatches = node.title.toLowerCase().includes(filterText);
        let filteredChildren = [];
        
        if (node.children && node.children.length > 0) {
            filteredChildren = filterTreeNodes(node.children, filterText);
        }
        
        // Include node if it matches or has matching children
        if (nodeMatches || filteredChildren.length > 0) {
            const filteredNode = { ...node };
            
            if (filteredChildren.length > 0) {
                filteredNode.children = filteredChildren;
                filteredNode.isExpandable = true;
            }
            
            // Highlight matching text
            if (nodeMatches) {
                filteredNode.title = highlightText(node.title, filterText);
            }
            
            filtered.push(filteredNode);
        }
    }
    
    return filtered;
}

function highlightText(text, searchText) {
    if (!searchText) return text;
    
    const regex = new RegExp(`(${escapeRegex(searchText)})`, 'gi');
    return text.replace(regex, '<mark>$1</mark>');
}

function escapeRegex(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function clearTreeFilter() {
    $('#treeFilter').val('');
    treeData = JSON.parse(JSON.stringify(originalTreeData));
    renderTree(treeData);
    updateFilterResults();
}

function updateFilterResults(filterText = '') {
    const resultsElement = $('#filterResults');
    
    if (!filterText) {
        const totalNodes = countNodes(originalTreeData);
        resultsElement.text(`${totalNodes.folders} carpetas, ${totalNodes.files} archivos`);
        return;
    }
    
    const filteredNodes = countNodes(treeData);
    const totalNodes = countNodes(originalTreeData);
    
    resultsElement.html(`
        Mostrando ${filteredNodes.folders} de ${totalNodes.folders} carpetas, 
        ${filteredNodes.files} de ${totalNodes.files} archivos
    `);
}

function countNodes(nodes) {
    let folders = 0;
    let files = 0;
    
    function countRecursive(nodeList) {
        for (const node of nodeList) {
            if (node.isCategory) {
                folders++;
            } else {
                files++;
            }
            
            if (node.children && node.children.length > 0) {
                countRecursive(node.children);
            }
        }
    }
    
    countRecursive(nodes);
    return { folders, files };
}

function expandAllNodes() {
    $('.tree-node .expand-icon').each(function() {
        const $icon = $(this);
        const $childrenContainer = $icon.closest('.tree-node').find('.tree-children').first();
        
        if ($childrenContainer.length > 0 && !$childrenContainer.is(':visible')) {
            $childrenContainer.show();
            $icon.removeClass('bi-chevron-right').addClass('bi-chevron-down');
        }
    });
}

function collapseAllNodes() {
    $('.tree-node .expand-icon').each(function() {
        const $icon = $(this);
        const $childrenContainer = $icon.closest('.tree-node').find('.tree-children').first();
        
        if ($childrenContainer.length > 0 && $childrenContainer.is(':visible')) {
            $childrenContainer.hide();
            $icon.removeClass('bi-chevron-down').addClass('bi-chevron-right');
        }
    });
}

// ============================================
// PERMISSIONS MANAGEMENT
// ============================================

function showPermissionsForFolder(categoryId, folderName) {
    // Update UI
    $('#permissionsFolderName').text(folderName);
    $('#permissionsWelcome').addClass('d-none');
    $('#permissionsMatrix').addClass('d-none');
    $('#permissionsLoading').removeClass('d-none');

    // Load permissions data
    $.get(getActionUrl('GetFolderPermissions', 'Documents'), { categoryId: categoryId })
        .done(function(permissions) {
            renderPermissionsMatrix(permissions);
            $('#permissionsLoading').addClass('d-none');
            $('#permissionsMatrix').removeClass('d-none');
        })
        .fail(function() {
            $('#permissionsLoading').addClass('d-none');
            showToast('error', 'Error al cargar los permisos');
        });
}

function renderPermissionsMatrix(permissions) {
    const tbody = $('#permissionsTableBody');
    tbody.empty();

    if (!permissions || permissions.length === 0) {
        tbody.html('<tr><td colspan="7" class="text-center text-muted py-4">No hay permisos configurados</td></tr>');
        return;
    }

    permissions.forEach(function(perm) {
        const row = $(`
            <tr data-role-id="${perm.roleId}">
                <td><strong>${perm.roleName}</strong></td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="view" ${perm.permissions.view ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="download" ${perm.permissions.download ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="upload" ${perm.permissions.upload ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="delete" ${perm.permissions.delete ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="modifyMetadata" ${perm.permissions.modifyMetadata ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input permission-checkbox" 
                           data-permission="admin" ${perm.permissions.admin ? 'checked' : ''} 
                           ${perm.isInherited ? 'disabled' : ''}>
                </td>
            </tr>
        `);

        // Add styling for inherited permissions
        if (perm.isInherited) {
            row.addClass('table-secondary');
            row.find('td:first').append(' <small class="text-muted">(heredado)</small>');
        }

        tbody.append(row);
    });

    // Add event listeners for permission changes
    $('.permission-checkbox').on('change', function() {
        const $row = $(this).closest('tr');
        const roleId = $row.data('role-id');
        updatePermissions(currentFolderId, roleId, $row);
    });
}

function updatePermissions(categoryId, roleId, $row) {
    const permissions = {
        view: $row.find('[data-permission="view"]').is(':checked'),
        download: $row.find('[data-permission="download"]').is(':checked'),
        upload: $row.find('[data-permission="upload"]').is(':checked'),
        delete: $row.find('[data-permission="delete"]').is(':checked'),
        modifyMetadata: $row.find('[data-permission="modifyMetadata"]').is(':checked'),
        admin: $row.find('[data-permission="admin"]').is(':checked')
    };

    const postData = {
        categoryId: categoryId,
        roleId: roleId,
        ...permissions,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    };

    $.post(getActionUrl('UpdateFolderPermissions', 'Documents'), postData)
        .done(function(response) {
            if (response.success) {
                showToast('success', response.message);
            } else {
                showToast('error', response.message);
                // Revert the change if it failed
                showPermissionsForFolder(categoryId, currentFolderName);
            }
        })
        .fail(function() {
            showToast('error', 'Error al actualizar los permisos');
            // Revert the change if it failed
            showPermissionsForFolder(categoryId, currentFolderName);
        });
}

// Hook into tab switching to load permissions when tab is activated
$('#permissions-tab').on('shown.bs.tab', function() {
    if (currentFolderId && currentFolderName) {
        showPermissionsForFolder(currentFolderId, currentFolderName);
    }
});

// ============================================
// AUDIT TRAIL FUNCTIONALITY
// ============================================

function showFileAuditTrail(fileId, fileName) {
    // Load the modal first
    $.get(getActionUrl('FileAuditTrail', 'Documents'))
        .done(function(response) {
            $('#modalContainer').html(response);
            
            // Set file name in modal
            $('#auditFileName').text(fileName);
            
            // Show modal
            var modal = new bootstrap.Modal(document.getElementById('fileAuditTrailModal'));
            modal.show();
            
            // Load audit data
            loadFileAuditData(fileId);
            
            // Setup filter event handlers
            setupAuditTrailFilters();
        })
        .fail(function() {
            showToast('error', 'Error al cargar el formulario de auditoría');
        });
}

function loadFileAuditData(fileId) {
    $('#auditTrailLoading').removeClass('d-none');
    $('#auditTrailContent, #auditTrailEmpty').addClass('d-none');
    
    $.get(getActionUrl('GetFileAuditData', 'Documents'), { fileId: fileId })
        .done(function(response) {
            if (response.success) {
                populateFileInfo(response.file);
                populateAuditTrail(response.auditTrail);
                
                $('#auditTrailLoading').addClass('d-none');
                
                if (response.auditTrail && response.auditTrail.length > 0) {
                    $('#auditTrailContent').removeClass('d-none');
                } else {
                    $('#auditTrailEmpty').removeClass('d-none');
                }
            } else {
                $('#auditTrailLoading').addClass('d-none');
                showToast('error', response.message || 'Error al cargar los datos de auditoría');
            }
        })
        .fail(function() {
            $('#auditTrailLoading').addClass('d-none');
            showToast('error', 'Error al cargar los datos de auditoría');
        });
}

function populateFileInfo(fileInfo) {
    $('#auditFileNameDetail').text(fileInfo.name);
    $('#auditFileSize').text(fileInfo.size);
    $('#auditFileType').text(fileInfo.type);
    $('#auditUploadedBy').text(fileInfo.uploadedBy);
    $('#auditUploadedDate').text(fileInfo.uploadedDate);
    $('#auditFolderPath').text(fileInfo.folderPath);
}

function populateAuditTrail(auditTrail) {
    const tbody = $('#auditTrailTableBody');
    tbody.empty();
    
    if (!auditTrail || auditTrail.length === 0) {
        return;
    }
    
    auditTrail.forEach(function(audit) {
        const resultBadge = getResultBadge(audit.result);
        const eventIcon = getEventIcon(audit.eventType, audit.action);
        const ipLocation = audit.ipAddress ? audit.ipAddress : 'No disponible';
        
        const row = $(`
            <tr data-audit-category="${audit.category}">
                <td class="fw-bold">${audit.eventDateTime}</td>
                <td>${audit.userName}</td>
                <td>
                    <i class="bi ${eventIcon} me-1"></i>
                    ${audit.action}
                </td>
                <td>${resultBadge}</td>
                <td class="audit-detail-cell" title="${audit.details}">
                    ${audit.details || 'Sin detalles'}
                </td>
                <td class="text-muted small">
                    ${ipLocation}
                </td>
            </tr>
        `);
        
        tbody.append(row);
    });
}

function getResultBadge(result) {
    const resultLower = (result || '').toLowerCase();
    
    switch (resultLower) {
        case 'success':
        case 'exitoso':
        case 'ok':
            return '<span class="badge bg-success audit-badge">Exitoso</span>';
        case 'failed':
        case 'error':
        case 'fallido':
            return '<span class="badge bg-danger audit-badge">Fallido</span>';
        case 'warning':
        case 'advertencia':
            return '<span class="badge bg-warning audit-badge">Advertencia</span>';
        case 'info':
        case 'información':
            return '<span class="badge bg-info audit-badge">Info</span>';
        default:
            return '<span class="badge bg-secondary audit-badge">' + (result || 'N/A') + '</span>';
    }
}

function getEventIcon(eventType, action) {
    const actionLower = (action || '').toLowerCase();
    const eventLower = (eventType || '').toLowerCase();
    
    if (actionLower.includes('download') || actionLower.includes('descargar')) {
        return 'bi-download';
    } else if (actionLower.includes('upload') || actionLower.includes('subir')) {
        return 'bi-upload';
    } else if (actionLower.includes('delete') || actionLower.includes('eliminar')) {
        return 'bi-trash';
    } else if (actionLower.includes('view') || actionLower.includes('ver') || actionLower.includes('access')) {
        return 'bi-eye';
    } else if (actionLower.includes('permission') || actionLower.includes('permiso')) {
        return 'bi-shield-lock';
    } else if (actionLower.includes('login') || actionLower.includes('authentication')) {
        return 'bi-person-check';
    } else if (eventLower.includes('security') || eventLower.includes('seguridad')) {
        return 'bi-exclamation-triangle';
    } else {
        return 'bi-activity';
    }
}

function setupAuditTrailFilters() {
    $('input[name="auditFilter"]').on('change', function() {
        const filter = $(this).attr('id');
        filterAuditTrail(filter);
    });
}

function filterAuditTrail(filter) {
    const rows = $('#auditTrailTableBody tr');
    
    rows.each(function() {
        const row = $(this);
        const category = row.data('audit-category');
        
        let shouldShow = true;
        
        switch (filter) {
            case 'allEvents':
                shouldShow = true;
                break;
            case 'accessEvents':
                shouldShow = category === 'access';
                break;
            case 'modificationEvents':
                shouldShow = category === 'modification';
                break;
            case 'securityEvents':
                shouldShow = category === 'security';
                break;
            default:
                shouldShow = true;
        }
        
        if (shouldShow) {
            row.show();
        } else {
            row.hide();
        }
    });
}

function exportAuditTrail() {
    // Get visible rows
    const visibleRows = $('#auditTrailTableBody tr:visible');
    const fileName = $('#auditFileName').text();
    
    if (visibleRows.length === 0) {
        showToast('warning', 'No hay datos para exportar');
        return;
    }
    
    // Create CSV content
    let csvContent = 'Fecha/Hora,Usuario,Acción,Resultado,Detalles,IP/Ubicación\n';
    
    visibleRows.each(function() {
        const cells = $(this).find('td');
        const row = [
            cells.eq(0).text(),
            cells.eq(1).text(),
            cells.eq(2).text().replace(/\s+/g, ' '),
            cells.eq(3).text(),
            '"' + cells.eq(4).attr('title').replace(/"/g, '""') + '"',
            cells.eq(5).text()
        ];
        csvContent += row.join(',') + '\n';
    });
    
    // Download CSV
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', `auditoria_${fileName}_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    showToast('success', 'Auditoría exportada exitosamente');
}

// ============================================
// UTILITY FUNCTIONS
// ============================================

function getActionUrl(action, controller) {
    // Use server URLs if available, otherwise fall back to direct construction
    if (window.serverUrls) {
        const actionKey = action.charAt(0).toLowerCase() + action.slice(1);
        if (window.serverUrls[actionKey]) {
            return window.serverUrls[actionKey];
        }
    }
    return `/${controller}/${action}`;
}