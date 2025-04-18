var dataTable;

$(document).ready(function () {
    loadDataTable();
});

// Load DataTable
function loadDataTable() {
    dataTable = $('#productTable').DataTable({
        "ajax": {
            url: '/admin/product/getall',
            dataSrc: function(json) {
                console.log(json);
                return json;
            }
        },
        "columns": [
            { data: 'title', width: '25%' },
            { data: 'isbn', width: '15%' },
            { data: 'listPrice', width: '10%' },
            { data: 'author', width: '15%' },
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
                            <a href="/Admin/Product/Upsert/${data}" class="btn btn-success mx-2" style="cursor:pointer;">
                                <i class="bi bi-pencil-square"></i> Edit
                            </a>
                            <a onclick=Delete("/Admin/Product/Delete/${data}") class="btn btn-danger mx-2" style="cursor:pointer;">
                                <i class="bi bi-trash-fill"></i> Delete
                            </a>
                        </div>
                    `;
                },
                "width": "25%"
            }
        ]
    });
}


function Delete(url)
{
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
      }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            });
        }
      });
}