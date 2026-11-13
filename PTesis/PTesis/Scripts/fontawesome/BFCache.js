window.addEventListener('pageshow', function (event) {
    var navigationType = window.performance && window.performance.getEntriesByType
        ? window.performance.getEntriesByType("navigation")[0]?.type
        : null;

    if (event.persisted || navigationType === "back_forward") {
        window.location.reload();
    }
});