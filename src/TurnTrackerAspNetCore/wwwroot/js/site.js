
$('.task-row').click(function(event) {
    window.location.href = $(event.currentTarget).find('.details-btn')[0].href;
});