from time import sleep
import math

print()
print("This is a smol script to test terminal auto scroll :)")
print("Press Ctrl + C to stop")
print()

DOUBLE_PI = math.pi * 2

left_offset_spaces = 26

current_angle = 0
angle_increment = 0.2
width = 30

stars_angles = [0, DOUBLE_PI / 3, DOUBLE_PI * 2 / 3]

while True:
    try:
        current_angle += angle_increment
        current_angle = current_angle - DOUBLE_PI if current_angle > DOUBLE_PI else current_angle

        buffer_size = width + 1
        zbuffer = [-10 for i in range(buffer_size)]
        buffer = [" " for i in range(buffer_size)]

        for star_angle in stars_angles:
            current_star_angle = current_angle + star_angle

            x = max(0, min(width, round((math.sin(current_star_angle) + 1) * width / 2)))
            y = math.cos(current_star_angle)

            if y < zbuffer[x]:
                continue

            char = '`'
            if y >= 0.6:
                char = '@'
            elif y >= 0:
                char = '*'
            elif y >= -0.6:
                char = '^'

            buffer[x] = char

        print(" " * left_offset_spaces + "".join(buffer))

        sleep(0.2)
    except KeyboardInterrupt:
        break

print()
print("bye o/\033[0m")
print()
