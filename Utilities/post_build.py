import os
import shutil

def cp(src, dst):
    print("Copying \"" + src + "\" to \"" + dst + "\"... ", end="")
    shutil.copyfile(src, dst)
    print("Done")

def mkdir(path):
    print("Creating directory \"" + path + "\"... ", end="")

    if os.path.exists(path):
        print("already exists, skip")
        return

    os.mkdir(path)

    print("Done")

if __name__ == "__main__":
    print("Starting post_build.py")

    cp("./Assets/Sample/settings.jsonc", "./Build/settings.jsonc")
    cp("./Assets/Sample/state.json", "./Build/state.json")
    cp("./Assets/Sample/keybinds.json", "./Build/keybinds.json")

    mkdir("./Build/Utilities")

    cp("./Utilities/color_test.py", "./Build/Utilities/color_test.py")
    cp("./Utilities/scroll_test.py", "./Build/Utilities/scroll_test.py")

    print("Done post_build.py")
