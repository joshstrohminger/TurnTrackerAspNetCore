
$('.task-row').click(function(event) {
    window.location.href = $(event.currentTarget).find('.details-btn')[0].href;
});

$('.delete-turn button').click(function (event) {
    var modal = $("#delete-turn-modal");
    var url = event.currentTarget.form.action;
    var tr = $(event.currentTarget).closest('tr');
    var taker = tr.find('.taker').text();
    var taken = tr.find('.taker').text();
    modal.find('#modal-turn-taker').text(taker);
    modal.find('#modal-turn-taken').text(taken);
    modal.find('form').attr('action', url);
    modal.modal('show');
});

$('.delete-user button').click(function (event) {
    var modal = $("#delete-user-modal");
    var url = event.currentTarget.form.action;
    var tr = $(event.currentTarget).closest('tr');
    var name = tr.find('.username').text();
    modal.find('#modal-user').text(name);
    modal.find('form').attr('action', url);
    modal.modal('show');
});