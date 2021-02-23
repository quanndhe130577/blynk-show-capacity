

-- File: Test.lua
function onInit()
	--print("Hello world")
end

function onDeviceStateChanged(device, state, stateValue)
  local timestamp = os.date("%Y-%m-%d %H:%M:%S")
  if stateValue == nil or stateValue=="" then
    print('{"type":"device","time":"'..timestamp..'","event":"state","id":'..device:id()..',"state":'..state..',"name":"'..device:name()..'"}')
  else
    print('{"type":"device","time":"'..timestamp..'","event":"state","id":'..device:id()..',"state":'..state..',"stateValue":"'..stateValue..',"name":"'..device:name()..'"}')
  end                  
end

function onSensorValueUpdated(device, valueType, value, scale)
  local timestamp = os.date("%Y-%m-%d %H:%M:%S")
  print('{"type":"sensor","time":"'..timestamp..'","event":"value","id":'..device:id()..',"valueType":'..valueType..',"value":'..value..',"scale":'..scale..',"name":"'..device:name()..'"}')
end
