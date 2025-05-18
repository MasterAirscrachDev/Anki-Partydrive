import numpy as np
from PIL import Image
import sys

def make_dark_grey_transparent(input_path, output_path, threshold=127, rgb_sum_threshold=20):
    """
    Load an image and make pixels transparent if they:
    1. Are grayscale and darker than threshold, OR
    2. Have a sum of RGB values less than rgb_sum_threshold
    
    Args:
        input_path (str): Path to the input image file
        output_path (str): Path where to save the processed image
        threshold (int): Brightness threshold (0-255), pixels below this will be made transparent
                         if they are grayscale. Default is 127 (50% brightness).
        rgb_sum_threshold (int): Threshold for sum of RGB values. Pixels with sum below this
                                will be made transparent regardless of grayscale. Default is 20.
    """
    try:
        # Open the image
        img = Image.open(input_path)
        
        # Convert to RGBA if it's not already
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        
        # Convert to numpy array for easier processing
        pixels = np.array(img)
        
        # Create a mask for grayscale pixels
        # A pixel is grayscale if R=G=B
        r, g, b, a = pixels[:,:,0], pixels[:,:,1], pixels[:,:,2], pixels[:,:,3]
        grayscale_mask = (r == g) & (g == b)
        
        # Create a mask for dark pixels
        dark_mask = r < threshold
        
        # Create a mask for pixels with low RGB sum
        rgb_sum = r.astype(int) + g.astype(int) + b.astype(int)
        low_sum_mask = rgb_sum < rgb_sum_threshold
        
        # Combine masks: (grayscale AND dark) OR low_sum
        combined_mask = (grayscale_mask & dark_mask) | low_sum_mask
        
        # Set alpha channel to 0 (transparent) for pixels that match the condition
        pixels[:,:,3] = np.where(combined_mask, 0, a)
        
        # Create a new image from the modified array
        result_img = Image.fromarray(pixels)
        
        # Save the result
        result_img.save(output_path)
        print(f"Image processed successfully and saved to {output_path}")
        
    except Exception as e:
        print(f"Error processing image: {e}")
        return None

if __name__ == "__main__":
    # Check if command line arguments are provided
    if len(sys.argv) >= 3:
        input_path = sys.argv[1]
        output_path = sys.argv[2]
        # Check if threshold is provided
        threshold = int(sys.argv[3]) if len(sys.argv) >= 4 else 127
        # Check if rgb_sum_threshold is provided
        rgb_sum_threshold = int(sys.argv[4]) if len(sys.argv) >= 5 else 20
        make_dark_grey_transparent(input_path, output_path, threshold, rgb_sum_threshold)
    else:
        print("Usage: python make_dark_grey_transparent.py input_image.png output_image.png [threshold] [rgb_sum_threshold]")
        print("Example: python make_dark_grey_transparent.py input.png output.png 100 20")