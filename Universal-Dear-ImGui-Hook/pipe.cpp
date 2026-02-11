#include <stdafx.h>
#include <windows.h>
#include <thread>
#include <string>
#include "pipe.h"
#include "Properties.h"
#include "Clipboard.h"


namespace pipe {

    bool cursorVisible = false;
    bool gameInitialized = false;
    bool openMenu = false;
    float generateProgress = 0.0f;

    void ProcessCommand(const std::string& cmd)
    {
        size_t pos = cmd.find("|");
        std::string command = cmd.substr(0, pos);
        std::string param = (pos != std::string::npos) ? cmd.substr(pos + 1) : "";

        DebugLog("[pipe] Command: %s, Param: %s\n", command.c_str(), param.c_str());

        ImGuiIO& io = ImGui::GetIO();

        if (command == "cursor")
        {
            if (param == "true")
            {
                cursorVisible = true;
            }
            else if (param == "false")
            {
                cursorVisible = false;
            }
        }
        else if (command == "game_initialized")
        {
            menu::isOpen = true;
            gameInitialized = true;
        }
        else if (command == "key_down")
        {
            if (param == "F2")
            {
                openMenu = !openMenu;
            }
        }
        else if (command == "generate_progress")
        {
            float progress = std::stof(param);
            generateProgress = progress;
        }
        else if (command == "properties_position_x")
        {
            Properties::positionX = std::stof(param);
        }
        else if (command == "properties_position_y")
        {
            Properties::positionY = std::stof(param);
        }
        else if (command == "properties_position_z")
        {
            Properties::positionZ = std::stof(param);
        }
        else if (command == "properties_rotation_x")
        {
            Properties::rotationX = std::stof(param);
        }
        else if (command == "properties_rotation_y")
        {
            Properties::rotationY = std::stof(param);
        }
        else if (command == "properties_rotation_z")
        {
            Properties::rotationZ = std::stof(param);
        }
        else if (command == "properties_object_id")
        {
            Properties::targetObjectId = param;
        }
        else if (command == "properties_is_model")
        {
            if (param == "true")
            {
                Properties::isModel = true;
            }
            else if (param == "false")
            {
                Properties::isModel = false;
            }
        }
        else if (command == "clipboard_preview_paste_model")
        {
            if (param == "true")
            {
                Properties::isPrevievPaste = true;
            }
            else if (param == "false")
            {
                Properties::isPrevievPaste = false;
            }
        }
    }

    void ListenForCommands()
    {
        std::thread([]() {
            while (true) {
                HANDLE hPipe = CreateNamedPipeA(
                    "\\\\.\\pipe\\KogamaStudio_CSharp",
                    PIPE_ACCESS_INBOUND,
                    PIPE_TYPE_BYTE,
                    1, 0, 0, 0, NULL
                );

                if (hPipe == INVALID_HANDLE_VALUE) continue;

                if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED) {
                    char buffer[256] = { 0 };
                    DWORD bytesRead = 0;

                    if (ReadFile(hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL))
                    {
                        buffer[bytesRead] = '\0';
                        ProcessCommand(std::string(buffer));
                    }
                }
                CloseHandle(hPipe);
            }
            }).detach();
    }
}