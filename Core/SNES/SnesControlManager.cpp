#include "stdafx.h"
#include "SNES/SnesControlManager.h"
#include "SNES/SnesConsole.h"
#include "SNES/SnesMemoryManager.h"
#include "Shared/Emulator.h"
#include "Shared/EmuSettings.h"
#include "Shared/KeyManager.h"
#include "Shared/Interfaces/IKeyManager.h"
#include "Shared/Interfaces/IInputProvider.h"
#include "Shared/Interfaces/IInputRecorder.h"
#include "SNES/Input/SnesController.h"
#include "SNES/Input/SnesMouse.h"
#include "SNES/Input/Multitap.h"
#include "SNES/Input/SuperScope.h"
#include "EventType.h"
#include "Utilities/Serializer.h"
#include "Shared/SystemActionManager.h"

SnesControlManager::SnesControlManager(SnesConsole* console) : BaseControlManager(console->GetEmulator())
{
	_console = console;
	UpdateControlDevices();
}

SnesControlManager::~SnesControlManager()
{
}

shared_ptr<BaseControlDevice> SnesControlManager::CreateControllerDevice(ControllerType type, uint8_t port)
{
	shared_ptr<BaseControlDevice> device;

	SnesConfig& cfg = _emu->GetSettings()->GetSnesConfig();

	switch(type) {
		default:
		case ControllerType::None: break;

		case ControllerType::SnesController:
			device.reset(new SnesController(_emu, port, port == 0 ? cfg.Port1.Keys : cfg.Port2.Keys));
			break;

		case ControllerType::SnesMouse: device.reset(new SnesMouse(_emu, port)); break;

		case ControllerType::SuperScope: device.reset(new SuperScope(_console, port, port == 0 ? cfg.Port1.Keys : cfg.Port2.Keys)); break;
		
		case ControllerType::Multitap: {
			ControllerConfig controllers[4];
			if(port == 0) {
				std::copy(cfg.Port1SubPorts, cfg.Port1SubPorts + 4, controllers);
				controllers[0].Keys = cfg.Port1.Keys;
			} else {
				std::copy(cfg.Port2SubPorts, cfg.Port2SubPorts + 4, controllers);
				controllers[0].Keys = cfg.Port2.Keys;
			}
			device.reset(new Multitap(_console, port, controllers));
			break;
		}
	}

	return device;
}

void SnesControlManager::UpdateControlDevices()
{
	SnesConfig& cfg = _emu->GetSettings()->GetSnesConfig();
	if(_emu->GetSettings()->IsEqual(_prevConfig, cfg) && _controlDevices.size() > 0) {
		//Do nothing if configuration is unchanged
		return;
	}

	auto lock = _deviceLock.AcquireSafe();
	ClearDevices();
	for(int i = 0; i < 2; i++) {
		shared_ptr<BaseControlDevice> device = CreateControllerDevice(i == 0 ? cfg.Port1.Type : cfg.Port2.Type, i);
		if(device) {
			RegisterControlDevice(device);
		}
	}
}

uint8_t SnesControlManager::Read(uint16_t addr)
{
	uint8_t value = _console->GetMemoryManager()->GetOpenBus() & (addr == 0x4016 ? 0xFC : 0xE0);
	for(shared_ptr<BaseControlDevice> &device : _controlDevices) {
		value |= device->ReadRam(addr);
	}

	return value;
}

void SnesControlManager::Write(uint16_t addr, uint8_t value)
{
	for(shared_ptr<BaseControlDevice> &device : _controlDevices) {
		device->WriteRam(addr, value);
	}
}

void SnesControlManager::Serialize(Serializer &s)
{
	if(!s.IsSaving()) {
		UpdateControlDevices();
	}

	for(shared_ptr<BaseControlDevice> &device : _controlDevices) {
		s.Stream(device.get());
	}
}
