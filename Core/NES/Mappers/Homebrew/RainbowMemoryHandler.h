#pragma once
#include "pch.h"
#include "NES/INesMemoryHandler.h"
#include "NES/NesConsole.h"
#include "NES/Mappers/Audio/RainbowAudio.h"

class RainbowMemoryHandler : public INesMemoryHandler
{
	RainbowAudio* _audio = nullptr;
	NesConsole* _console = nullptr;
	uint8_t _ppuRegs[8] = {};
	uint8_t _oamOffset;
	uint8_t _sprY[64];
	uint16_t _oamWrite;

public:
	RainbowMemoryHandler(NesConsole* console, RainbowAudio* audio)
	{
		_audio = audio;
		_console = console;
		_oamOffset = 2;
	}

	uint8_t GetPpuReg(uint16_t addr)
	{
		return _ppuRegs[addr & 0x07];
	}

	uint8_t GetSpriteYPos(uint8_t spriteIndex)
	{
		return _sprY[spriteIndex];
	}

	void GetMemoryRanges(MemoryRanges& ranges) override {}

	uint8_t ReadRam(uint16_t addr) override
	{
		if(addr == 0x4011) {
			if(_audio != nullptr && _audio->_audioOutput & 0x04)
				return _audio->_lastOutput << 1;
			else
				return _console->GetMemoryManager()->GetOpenBus();
		} else {
			return _console->GetMemoryManager()->GetInternalRam()[addr];
		}
	}
	
	void WriteRam(uint16_t addr, uint8_t value) override
	{
		_console->GetPpu()->WriteRam(addr, value);

		if(addr >= 0x2000 && addr <= 0x2007) {
			_ppuRegs[addr & 0x07] = value;
			if(addr == 0x2004) {
				uint8_t spriteIndex = (_oamWrite >> 2) & 0x3f;
				_sprY[spriteIndex] = value;
				_oamWrite++;
			}
		} else if(addr == 0x4014) {
			_oamOffset = value;
			_oamWrite = 0;
		}/* else if(addr == 0x4011) {
			//uint32_t out = _audio->_lastOutput;
			_console->GetMemoryManager()->Write(0x4011, 0xFF, MemoryOperationType::Write);
		}*/
	}
};
