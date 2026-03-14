#pragma once
#include <string>

//typedef void(*CommandCallback)(const char* command, const char* )

namespace pipe {
	extern bool cursorVisible;
	extern bool gameInitialized;
	extern bool openMenu;
	extern float generateProgress;
	extern bool isFullscreen;

	extern bool  isPasteBuilding;
	extern int   pastePlacedCubes;
	extern int   pasteTotalCubes;
	extern float pasteEtaSeconds;

	void ListenForCommands();
	void ProcessCommand(const std::string& cmd);
}