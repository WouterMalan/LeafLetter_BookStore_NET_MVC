var dataTable;

$(document).ready(() => {
    const url = window.location.search;
    const orderStatuses = ['inprocess', 'completed', 'pending', 'approved'];
    const status = orderStatuses.find(status => url.includes(status)) || 'all';
    loadDataTable(status);
});

function loadDataTable(status) {
    dataTable = $('#orderTable').DataTable({
        "ajax": {
            url: '/admin/order/getall?status=' + status
        },
        "columns": [
            { 
                data: 'id',
                width: '10%'
            },
            { 
                data: 'name',
                width: '15%'
            },
            { 
                data: 'phoneNumber',
                width: '15%'
            },
            { 
                data: 'applicationUser.email',
                width: '20%'
            },
            { 
                data: 'orderStatus',
                width: '15%',
                render: function(data) {
                    let badgeClass = '';
                    let icon = '';
                    
                    switch(data.toLowerCase()) {
                        case 'pending':
                            badgeClass = 'bg-warning';
                            icon = 'clock';
                            break;
                        case 'approved':
                            badgeClass = 'bg-info';
                            icon = 'shield-check';
                            break;
                        case 'inprocess':
                            badgeClass = 'bg-primary';
                            icon = 'gear';
                            break;
                        case 'completed':
                            badgeClass = 'bg-success';
                            icon = 'check-circle';
                            break;
                        default:
                            badgeClass = 'bg-secondary';
                            icon = 'question-circle';
                    }
                    
                    return `<span class="badge ${badgeClass} order-status-badge">
                                <i class="bi bi-${icon} me-1"></i>${data}
                            </span>`;
                }
            },
            { 
                data: 'orderTotal',
                width: '10%',
                render: function(data) {
                    return `R${data.toFixed(2)}`;
                }
            },
            {
                data: 'id',
                width: '15%',
                "render": function (data) {
                    return `<div class="order-actions">
                        <a href="/Admin/order/details?orderId=${data}" 
                           class="btn btn-primary btn-sm">
                            <i class="bi bi-pencil-square me-1"></i>Details
                        </a>
                    </div>`;
                }
            }
        ],
        "order": [[0, "desc"]],
        "pageLength": 10,
        "language": {
            "emptyTable": "No orders found"
        }
    });
}
