#pragma once

std::string readFileContext(const std::string& fileName)
{
    const char* appData = std::getenv("APPDATA");
    if (!appData) {
        // Handle error: APPDATA environment variable not found.
        return "";
    }

    std::string path = std::string(appData) + "/Aeroshide/Jumbo-Josh/";
    std::ifstream file(path + fileName);
    if (!file) {
        // Handle error: file could not be opened.
        return "";
    }

    std::stringstream buffer;
    buffer << file.rdbuf();
    return buffer.str();
}