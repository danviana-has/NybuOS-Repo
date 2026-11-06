from PIL import Image

im = Image.open('wallpaper.bmp') # 1280x720
im_resized = im.resize((1920, 1080), Image.BILINEAR)
im_resized.save('test_resized.bmp')
print("Resized smoothly without any noise! Size:", im_resized.size)
