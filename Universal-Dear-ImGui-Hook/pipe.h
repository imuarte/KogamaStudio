#pragma once
#include <string>

//typedef void(*CommandCallback)(const char* command, const char* )

namespace pipe {
	extern bool cursorVisible;
	extern bool gameInitialized;
	extern bool openMenu;
	extern float generateProgress;

	void ListenForCommands();
	void ProcessCommand(const std::string& cmd);
}