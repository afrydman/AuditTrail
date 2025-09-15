// Audit Trail JavaScript Functions
// Version 1.0.0

// Global variables
let currentPage = 1;
let pageSize = 50;
let totalRecords = 0;
let currentAuditData = [];

// Initialize page
$(document).ready(function() {
    console.log('Audit page initialized');
    
    // Set up event listeners
    setupEventListeners();
    
    // Load initial data
    loadFilterOptions();
    
    // Set default date range if not set
    if (!$('#startDate').val()) {
        setDatePreset(7); // Default to last 7 days
    }
});

// Set up all event listeners
function setupEventListeners() {
    // Filter form changes
    $('#auditFiltersForm').on('change', 'select, input', function() {
        // Auto-search when filters change (with debounce)
        clearTimeout(window.filterTimeout);
        window.filterTimeout = setTimeout(searchAuditTrail, 500);
    });
    
    // Enter key in form triggers search
    $('#auditFiltersForm').on('keypress', 'input', function(e) {
        if (e.which === 13) {
            e.preventDefault();
            searchAuditTrail();
        }
    });
    
    // Table row click handlers
    $(document).on('click', '.audit-row-expandable', function() {
        toggleRowDetails($(this));
    });
    
    $(document).on('click', '.audit-detail-btn', function(e) {
        e.stopPropagation();
        const auditId = $(this).data('audit-id');
        showAuditDetailModal(auditId);
    });
}

// Load filter dropdown options via AJAX
function loadFilterOptions() {
    // Load event types
    $.get(window.auditUrls.getEventTypes)
        .done(function(response) {
            if (response.success && response.eventTypes) {
                const select = $('#eventTypeFilter');
                select.empty().append('<option value="">Todos los eventos</option>');
                response.eventTypes.forEach(function(eventType) {
                    select.append(`<option value="${eventType}">${eventType}</option>`);
                });
            }
        })
        .fail(function() {
            console.error('Error loading event types');
        });
    
    // Load entity types
    $.get(window.auditUrls.getEntityTypes)
        .done(function(response) {
            if (response.success && response.entityTypes) {
                const select = $('#entityTypeFilter');
                select.empty().append('<option value="">Todas las entidades</option>');
                response.entityTypes.forEach(function(entityType) {
                    select.append(`<option value="${entityType}">${entityType}</option>`);
                });
            }
        })
        .fail(function() {
            console.error('Error loading entity types');
        });
}

// Set date range preset
function setDatePreset(days) {
    const endDate = new Date();
    const startDate = new Date();
    
    if (days === 0) {
        // Today only
        startDate.setDate(endDate.getDate());
    } else {
        // Days ago
        startDate.setDate(endDate.getDate() - days);
    }
    
    $('#startDate').val(formatDateForInput(startDate));
    $('#endDate').val(formatDateForInput(endDate));
    
    // Trigger search
    searchAuditTrail();
}

// Format date for input field (YYYY-MM-DD)
function formatDateForInput(date) {
    return date.toISOString().split('T')[0];
}

// Clear all filters
function clearAllFilters() {
    $('#auditFiltersForm')[0].reset();
    
    // Set default date range
    setDatePreset(7);
    
    // Clear dropdowns
    $('#userFilter').val('');
    $('#eventTypeFilter').val('');
    $('#entityTypeFilter').val('');
    $('#resultFilter').val('');
    
    // Search with cleared filters
    searchAuditTrail();
}

// Main search function
function searchAuditTrail(page = 1) {
    currentPage = page;
    
    // Show loading state
    showLoadingState();
    
    // Gather filter values
    const filters = {
        startDate: $('#startDate').val() || null,
        endDate: $('#endDate').val() || null,
        userId: $('#userFilter').val() || null,
        eventType: $('#eventTypeFilter').val() || null,
        entityType: $('#entityTypeFilter').val() || null,
        result: $('#resultFilter').val() || null,
        page: currentPage,
        pageSize: pageSize
    };
    
    console.log('Searching with filters:', filters);
    
    // Make AJAX request
    $.get(window.auditUrls.searchAuditData, filters)
        .done(function(response) {
            console.log('Search response:', response);
            
            if (response.success) {
                currentAuditData = response.data;
                totalRecords = response.totalRecords;
                
                if (currentAuditData.length > 0) {
                    renderAuditTable(currentAuditData);
                    updateResultsCount(currentAuditData.length);
                    showTableView();
                } else {
                    showEmptyState();
                }
            } else {
                showError(response.message || 'Error al buscar datos de auditoría');
            }
        })
        .fail(function(xhr, status, error) {
            console.error('AJAX error:', status, error);
            showError('Error de conexión al buscar datos de auditoría');
        });
}

// Show loading state
function showLoadingState() {
    $('#auditLoading').removeClass('d-none');
    $('#auditTableView').addClass('d-none');
    $('#auditEmptyState').addClass('d-none');
    $('#auditPagination').hide();
}

// Show table view
function showTableView() {
    $('#auditLoading').addClass('d-none');
    $('#auditTableView').removeClass('d-none');
    $('#auditEmptyState').addClass('d-none');
    $('#auditPagination').show();
}

// Show empty state
function showEmptyState() {
    $('#auditLoading').addClass('d-none');
    $('#auditTableView').addClass('d-none');
    $('#auditEmptyState').removeClass('d-none');
    $('#auditPagination').hide();
    updateResultsCount(0);
}

// Show error message
function showError(message) {
    $('#auditLoading').addClass('d-none');
    $('#auditTableView').addClass('d-none');
    $('#auditEmptyState').removeClass('d-none').find('h5').text('Error').next('p').html(`<span class="text-danger">${message}</span>`);
    $('#auditPagination').hide();
}

// Render audit table
function renderAuditTable(auditData) {
    const tbody = $('#auditTableBody');
    tbody.empty();
    
    auditData.forEach(function(entry, index) {
        const row = createAuditTableRow(entry, index);
        tbody.append(row);
    });
}

// Create single audit table row
function createAuditTableRow(entry, index) {
    const resultClass = getResultClass(entry.result);
    const resultIcon = getResultIcon(entry.result);
    const actionIcon = getActionIcon(entry.action);
    
    return `
        <tr class="audit-row-expandable" data-audit-id="${entry.auditId}" data-index="${index}">
            <td class="text-center">
                <button class="btn btn-sm btn-outline-secondary border-0 expand-btn" data-bs-toggle="tooltip" title="Ver detalles">
                    <i class="bi bi-chevron-right"></i>
                </button>
            </td>
            <td class="text-nowrap small">
                <div>${entry.eventDateTime}</div>
            </td>
            <td class="text-nowrap">
                <strong>${entry.userName}</strong>
            </td>
            <td>
                <span class="badge ${getCategoryBadgeClass(entry.eventCategory)}">${entry.eventType}</span>
            </td>
            <td class="text-nowrap">
                <i class="bi ${actionIcon} me-1"></i>
                <span class="small">${entry.action}</span>
            </td>
            <td class="text-center">
                <i class="bi ${resultIcon} ${resultClass}"></i>
                <div class="small ${resultClass}">${entry.result}</div>
            </td>
            <td class="small">${entry.entityType}</td>
            <td class="small">${entry.entityName}</td>
            <td class="text-nowrap small">${entry.ipAddress}</td>
            <td class="text-nowrap small">${entry.duration}</td>
        </tr>
    `;
}

// Toggle row details
function toggleRowDetails(row) {
    const auditId = row.data('audit-id');
    const index = row.data('index');
    const entry = currentAuditData[index];
    
    const detailRow = row.next('.audit-detail-row');
    const expandBtn = row.find('.expand-btn i');
    
    if (detailRow.length) {
        // Detail row exists, toggle it
        detailRow.toggle();
        expandBtn.toggleClass('bi-chevron-right bi-chevron-down');
    } else {
        // Create detail row
        const detailRowHtml = createAuditDetailRow(entry);
        row.after(detailRowHtml);
        expandBtn.removeClass('bi-chevron-right').addClass('bi-chevron-down');
    }
}

// Create detailed row content
function createAuditDetailRow(entry) {
    const hasChanges = entry.oldValue || entry.newValue;
    const hasError = entry.errorMessage;
    
    let changesHtml = '';
    if (hasChanges) {
        changesHtml = `
            <div class="col-12">
                <h6>Cambios de Valor:</h6>
                <div class="value-change">
                    ${entry.oldValue ? `<span class="old-value">Anterior: ${entry.oldValue}</span>` : ''}
                    ${entry.newValue ? `<span class="new-value">Nuevo: ${entry.newValue}</span>` : ''}
                </div>
            </div>
        `;
    }
    
    let errorHtml = '';
    if (hasError) {
        errorHtml = `
            <div class="col-12">
                <h6>Mensaje de Error:</h6>
                <div class="alert alert-danger small mb-0">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    ${entry.errorMessage}
                </div>
            </div>
        `;
    }
    
    return `
        <tr class="audit-detail-row">
            <td colspan="10">
                <div class="audit-detail-content">
                    <div class="row g-3">
                        <div class="col-md-6">
                            <h6>Información del Evento:</h6>
                            <table class="table table-sm audit-detail-table">
                                <tbody>
                                    <tr>
                                        <th>ID de Auditoría:</th>
                                        <td><code>${entry.auditId}</code></td>
                                    </tr>
                                    <tr>
                                        <th>Categoría:</th>
                                        <td>${entry.eventCategory}</td>
                                    </tr>
                                    <tr>
                                        <th>Tipo de Evento:</th>
                                        <td>${entry.eventType}</td>
                                    </tr>
                                    <tr>
                                        <th>Acción:</th>
                                        <td>${entry.action}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>Información de la Entidad:</h6>
                            <table class="table table-sm audit-detail-table">
                                <tbody>
                                    <tr>
                                        <th>Tipo:</th>
                                        <td>${entry.entityType}</td>
                                    </tr>
                                    <tr>
                                        <th>Nombre:</th>
                                        <td>${entry.entityName}</td>
                                    </tr>
                                    <tr>
                                        <th>Usuario:</th>
                                        <td>${entry.userName}</td>
                                    </tr>
                                    <tr>
                                        <th>IP:</th>
                                        <td>${entry.ipAddress}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        ${changesHtml}
                        ${errorHtml}
                    </div>
                    <div class="row mt-3">
                        <div class="col-12 text-end">
                            <button class="btn btn-sm btn-outline-primary audit-detail-btn" data-audit-id="${entry.auditId}">
                                <i class="bi bi-zoom-in me-1"></i>
                                Ver Detalles Completos
                            </button>
                        </div>
                    </div>
                </div>
            </td>
        </tr>
    `;
}

// Helper functions for styling
function getResultClass(result) {
    switch (result) {
        case 'Success': return 'text-success';
        case 'Failed': return 'text-danger';
        case 'Warning': return 'text-warning';
        default: return 'text-muted';
    }
}

function getResultIcon(result) {
    switch (result) {
        case 'Success': return 'bi-check-circle-fill';
        case 'Failed': return 'bi-x-circle-fill';
        case 'Warning': return 'bi-exclamation-triangle-fill';
        default: return 'bi-info-circle';
    }
}

function getActionIcon(action) {
    if (action.includes('Login')) return 'bi-box-arrow-in-right';
    if (action.includes('Logout')) return 'bi-box-arrow-right';
    if (action.includes('Create')) return 'bi-plus-circle';
    if (action.includes('Update') || action.includes('Modify')) return 'bi-pencil';
    if (action.includes('Delete')) return 'bi-trash';
    if (action.includes('View')) return 'bi-eye';
    if (action.includes('Download')) return 'bi-download';
    if (action.includes('Upload')) return 'bi-upload';
    return 'bi-clock';
}

function getCategoryBadgeClass(category) {
    if (!category) return 'badge bg-light text-dark';
    if (category.includes('Security')) return 'badge bg-danger';
    if (category.includes('Document')) return 'badge bg-primary';
    if (category.includes('User')) return 'badge bg-info';
    if (category.includes('System')) return 'badge bg-secondary';
    return 'badge bg-light text-dark';
}

// Update results count
function updateResultsCount(count) {
    $('#resultsCount').text(count);
    $('#recordsShowing').text(`${count} de ${totalRecords}`);
}

// View switching functions
function switchView(viewType) {
    if (viewType === 'table') {
        $('#tableViewBtn').addClass('active');
        $('#dashboardViewBtn').removeClass('active');
        // Table view is already implemented
    } else if (viewType === 'dashboard') {
        $('#tableViewBtn').removeClass('active');
        $('#dashboardViewBtn').addClass('active');
        // TODO: Implement dashboard view in Phase 2
        alert('Vista de dashboard será implementada en la próxima fase');
        // Revert to table view
        switchView('table');
    }
}

// Row expansion functions
function expandAllRows() {
    $('.audit-row-expandable').each(function() {
        const row = $(this);
        if (!row.next('.audit-detail-row').length) {
            toggleRowDetails(row);
        }
    });
}

function collapseAllRows() {
    $('.audit-detail-row').hide();
    $('.expand-btn i').removeClass('bi-chevron-down').addClass('bi-chevron-right');
}

// Refresh data
function refreshAuditData() {
    searchAuditTrail(currentPage);
}

// Export function (placeholder)
function exportAuditTrail() {
    // TODO: Implement export functionality
    alert('Funcionalidad de exportación será implementada en una fase posterior');
}

// Show audit detail modal
function showAuditDetailModal(auditId) {
    const entry = currentAuditData.find(e => e.auditId === auditId);
    if (!entry) {
        console.error('Audit entry not found:', auditId);
        return;
    }
    
    const modalContent = `
        <div class="row g-3">
            <div class="col-12">
                <table class="table table-bordered audit-detail-table">
                    <tbody>
                        <tr><th>ID de Auditoría</th><td><code>${entry.auditId}</code></td></tr>
                        <tr><th>Fecha/Hora</th><td>${entry.eventDateTime}</td></tr>
                        <tr><th>Tipo de Evento</th><td><span class="badge ${getCategoryBadgeClass(entry.eventCategory)}">${entry.eventType}</span></td></tr>
                        <tr><th>Categoría</th><td>${entry.eventCategory}</td></tr>
                        <tr><th>Acción</th><td><i class="bi ${getActionIcon(entry.action)} me-2"></i>${entry.action}</td></tr>
                        <tr><th>Resultado</th><td><i class="bi ${getResultIcon(entry.result)} ${getResultClass(entry.result)} me-2"></i>${entry.result}</td></tr>
                        <tr><th>Usuario</th><td>${entry.userName}</td></tr>
                        <tr><th>Tipo de Entidad</th><td>${entry.entityType}</td></tr>
                        <tr><th>Nombre de Entidad</th><td>${entry.entityName}</td></tr>
                        <tr><th>Dirección IP</th><td>${entry.ipAddress}</td></tr>
                        <tr><th>Duración</th><td>${entry.duration}</td></tr>
                        ${entry.oldValue ? `<tr><th>Valor Anterior</th><td><pre class="small">${entry.oldValue}</pre></td></tr>` : ''}
                        ${entry.newValue ? `<tr><th>Valor Nuevo</th><td><pre class="small">${entry.newValue}</pre></td></tr>` : ''}
                        ${entry.errorMessage ? `<tr><th>Mensaje de Error</th><td class="text-danger">${entry.errorMessage}</td></tr>` : ''}
                    </tbody>
                </table>
            </div>
        </div>
        <div class="row mt-3">
            <div class="col-12">
                <div class="compliance-footer">
                    <i class="bi bi-shield-check"></i>
                    Registro de auditoría inmutable conforme a CFR 21 Part 11
                </div>
            </div>
        </div>
    `;
    
    $('#auditDetailContent').html(modalContent);
    $('#auditDetailModal').modal('show');
}