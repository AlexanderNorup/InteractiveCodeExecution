function getBootstrapColorFromSeverity(severity) {
    switch (severity) {
        default:
        case "debug": return "secondary";
        case "information": return "info";
        case "error": return "danger";
        case "warning": return "warning";
    }
}