using System;
using System.IO;
using SDL2;

namespace CHIP_8_SDL;

public static class Program
{
   static IntPtr renderer;
   static IntPtr font;
   //static string romPath = @"C:\Users\zaidg\Downloads\test_opcode.ch8";
   static string romPath = "/home/zaid/Downloads/3-corax+.ch8";


    public static void Main()
    {
        CPU chip = new CPU();
        chip.debugLoadRom(romPath);
        DoRender(chip);
    }

    static void DoRender(CPU c)
    {
        // Initialize SDL and TTF
        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        SDL_ttf.TTF_Init();

        IntPtr window = SDL.SDL_CreateWindow(
            "render that video array oorah",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            1400, 720,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
        );

        renderer = SDL.SDL_CreateRenderer(window, -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
            SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC
        );

        // Load font (change path if needed)
        font = SDL_ttf.TTF_OpenFont("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15); //on linux
        //font = SDL_ttf.TTF_OpenFont("C:/Windows/Fonts/comic.ttf", 15);

        bool quit = false;
        SDL.SDL_Event e;
        while (!quit)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    quit = true;
            }

            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);
            
            int bytesPerLine = 64; 
            int startX = (1400/4) + 40;        
            int startY = 25;       
            int lineHeight = 10;   
            
            RenderMemory(c, bytesPerLine, startX, startY, lineHeight);
            //RenderScreen(c, startX, startY, lineHeight);

            SDL.SDL_RenderPresent(renderer);
            SDL.SDL_Delay(16); // ~60 FPS
        }

        // cleanup
        SDL_ttf.TTF_CloseFont(font);
        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
        SDL_ttf.TTF_Quit();
        SDL.SDL_Quit();
    }
    
    static void RenderMemory(CPU c, int bytesPerLine, int startX, int startY, int lineHeight)
    {
        int totalBytes = 4096;
        for (int row = 0; row < totalBytes / bytesPerLine; row++)
        {
            int offset = row * bytesPerLine;

            // Build one line of memory in hex
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int col = 0; col < bytesPerLine; col++)
            {
                byte temp = c.dumpMemory(offset + col);
                sb.Append(temp.ToString("X2")).Append(""); // hex with space
            }

            // Render the entire line once
            RenderText(sb.ToString(), startX, startY + row * lineHeight);
        }
    }
    
    static void RenderText(string message, int x, int y)
    {
        SDL.SDL_Color white = new SDL.SDL_Color { r = 255, g = 255, b = 255, a = 255 };
        IntPtr surface = SDL_ttf.TTF_RenderText_Solid(font, message, white);
        IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, surface);

        SDL.SDL_QueryTexture(texture, out _, out _, out int w, out int h);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect { x = x, y = y, w = w, h = h };

        SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref dstRect);

        SDL.SDL_FreeSurface(surface);
        SDL.SDL_DestroyTexture(texture);
    }

    static void RenderScreen(CPU c, int x, int y, int space)
    {
        for (int row = 0; row < 64; row++)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int col = 0; col < 32; col++)
            {
                byte temp = c.debugShowScreen(row, col);
                
                sb.Append(temp.ToString("X2")).Append(""); // hex with space
            }
            RenderText(sb.ToString(), x, y + row * space);
        }
    }


}

public class CPU
{
    private ushort opCode; //stores the current opcode
    byte[] memory =  new byte[4096]; //represents 4 kilobytes of memory (this should be unsgined!)
    byte[] V = new byte[16]; //cpu registers, 16 total
    private ushort pc; //program counter, stores the next instruction
    private ushort i; //index register
    byte[,] gfx = new byte[64, 32]; //graphics representation, 2048 pixels
    private byte delayTimer;
    private byte soundTimer;
    ushort[] stack = new ushort[16];
    private byte sp; //stack pointer
    byte[] key = new byte[16]; //keeps the current state of the key
    
    

    private const uint fontSize = 80;
    byte[] fontSet =
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };

    public CPU()
    {
        pc = 0x200;; //thats where the program counter starts
        opCode = 0; //reset
        i = 0;
        sp = 0;
        LoadFont();
        
    }
    
    //rom loader

    void LoadROM(string filepath)
    {
        byte[] file = File.ReadAllBytes(filepath);

        if (file != null)
        {
            for (long i = 0; i < file.Length; i++)
            {
                memory[0x200 + i] = file[i]; //load rom from our starting address
            }
        }
        else
        {
            Console.WriteLine("File not found buddy");
        }
    }

    void LoadFont()
    {
        for (long i = 0; i < fontSize; i++)
        {
            memory[0x50 + i] = fontSet[i]; //load font from the font start address
        }
    }

    public byte dumpMemory(int ind)
    {
        return memory[ind];
    }

    public void debugLoadRom(string filepath)
    {
        LoadROM(filepath);
    }

    public byte debugShowScreen(int indx, int indy)
    {
        return gfx[indx, indy];
    }
    
    
    
    //note that Vx is a stand-in for our registers, where x is 0-F
    
    public void CLS(){ Array.Clear(gfx); } //CLS clear display

    public void RET() //RET return from routine
    {
        sp--;
        pc = stack[sp];
    } 
    public void JUMP(ushort addr){pc = addr;} //JP, jump to location nnn

    public void CALL(ushort addr)
    {
        sp++;
        stack[15] = pc;
        pc = addr;
    }
    public void SE_XKK(byte reg, byte kk){if(reg == kk){pc += 2;}} //skip if vx = a byte
    public void SNE_XKK(){} //skip if vx != a byte
    public void SE_XY(){} //skip if vx = vy
    public void LD_XKK(){} //sets vx to a byte
    public void ADD_XKK(){} //Vx = Vx + a byte
    public void LD_XY(){} //set vx to vy
    public void OR_XY(){} //set vx to vx OR vy
    public void AND_XY(){} //set vx to vx AND vy
    public void XOR_XY(){} //set vx to vx XOR vy (exclusive or)
    public void ADD_XY(){} //add two registers, if greater than 255 VF is set to 1
    public void SUB_XY(){} //subtract two registers (vx-vy stored in vx), if vx > vy, vf is 1. otherwise 0
    public void SHR_X(){} //set vx to vx SHR 1
    public void SUBN_XY(){} //set vx = vy- vx, vf will NOT borrow
    public void SHL_X(){} //set vx = vx SHL 1
    
}