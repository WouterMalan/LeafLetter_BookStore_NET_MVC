var dataTable;

$(document).ready(function () {
    loadDataTable();
});

// Load DataTable
function loadDataTable() {
    dataTable = $('#userTable').DataTable({
        "ajax": {
            url: '/admin/user/getall'
            // dataSrc: function(json) {
            //     console.log(json);
            //     return json;
            // }
        },
        "columns": [
            { data: 'name', width: '15%' },
            { data: 'email', width: '15%' },
            { data: 'phoneNumber', width: '15%' },
            { data: 'company.name', width: '15%' },
            { data: 'role', width: '15%' },
            {
                data: { id: 'id', lockoutEnd: 'lockoutEnd' },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();

                    if (lockout > today) {
                        return `
                        <div class="text-center">
                        <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white" style="cursor:pointer; width:100px;">
                        <i class="bi bi-unlock-fill"></i> Lock
                    </a>
                            <a class="btn btn-danger text-white" style="cursor:pointer; width:150px;">
                                <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        </div>
                    `
                    }
                    else
                    {
                        return `
                        <div class="text-center">
                        <a onclick=LockUnlock('${data.id}')class="btn btn-success text-white" style="cursor:pointer; width:100px;">
                        <i class="bi bi-unlock-fill"></i> Unlock
                    </a>
                        <a class="btn btn-danger text-white" style="cursor:pointer; width:150px;">
                            <i class="bi bi-pencil-square"></i> Permission
                        </a>
                    </div>
                    `
                    }
                    
                },
                "width": "25%"
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        type: 'POST',
        url: '/Admin/User/LockUnlock',
        contentType: 'application/json',
        data: JSON.stringify(id),
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
            else {
                toastr.error(data.message);
            }
        }
    })
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