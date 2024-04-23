var dataTable;

$(document).ready(function () {
    loadDataTable();
});

// Load DataTable
function loadDataTable() {
    dataTable = $('#productTable').DataTable({
        "ajax": {
            url: '/admin/order/getall'
            // dataSrc: function(json) {
            //     console.log(json);
            //     return json;
            // }
        },
        "columns": [
            { data: 'id', width: '5%' },
            { data: 'name', width: '15%' },
            { data: 'phoneNumber', width: '20%' },
            { data: 'applicationUser.email', width: '15%' },
            { data: 'orderStatus', width: '15%' },
            { data: 'orderTotal', width: '15%' },
            {
                data: 'category', 
                width: '10%',
                "render": function(data, type, row) {
                    return data ? data.name : '';
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a href="/Admin/order/details?orderId=${data}" class="btn btn-success mx-2" style="cursor:pointer;">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                        </div>
                    `;
                },
                "width": "25%"
            }
        ]
    });
}
