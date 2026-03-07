#pragma once
#include <string>
#include <vector>

namespace Explorer {
    struct ObjectEntry {
        std::string id;
        std::string type;
    };

    void LoadFromFile();
    void Render();
}
