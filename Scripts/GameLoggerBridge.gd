extends Node

# This is a singleton that will be autoloaded
func write_all_logs() -> void:
    # Call the C# GameLogger's WriteAllLogs method
    var gameLogger = Engine.get_singleton("GameLogger")
    if gameLogger:
        gameLogger.WriteAllLogs() 