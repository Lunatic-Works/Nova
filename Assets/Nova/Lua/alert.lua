function alert(text)
    stop_auto_ff()
    Nova.Alert.Show('', text)
end

function notify(text)
    Nova.Alert.Show(text)
end
