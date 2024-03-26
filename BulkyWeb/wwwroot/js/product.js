$(document).ready(function () {
    loadDataTable();
});

// Load DataTable
function loadDataTable() {
    dataTable = $('#productTable').DataTable({
        "ajax": {url:'/admin/product/getall'},
        "columns": [
            { data: 'title', width: '25%' },
            { data: 'isbn', width: '15%' },
            { data: 'listPrice', width: '10%' },
            { data: 'author', width: '20%' },
            { data: 'category.name', width: '15%' }
        ]
    });
}

