#pragma once
#include "stdafx.h"
#include <iostream>
#include <string>
#include <vector>

namespace Recovery {

    struct Problem {
        std::string description;
        std::string objectId;
        std::string actionType; // "remove", "teleport", or ""
    };

    void Render();
    void RenderOverlay();
    void AddProblem(const std::string& objectId, const std::string& actionType, const std::string& description);

    extern bool recoveryOpen;
    extern int targetItemId;
    extern bool corruptionDetected;
    extern bool corruptionDismissed;
}