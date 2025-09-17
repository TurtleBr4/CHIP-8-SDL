using System;
using System.IO;
using SDL2;


namespace CHIP_8_SDL;

public class Program
{
    public static void Main()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"SDL could not initialize! Error: {SDL.SDL_GetError()}");
            return;
        }

        IntPtr window = SDL.SDL_CreateWindow(
            "Holy shit lmao",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            800, 600,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
        );
        bool quit = false;
        SDL.SDL_Event e;

        while (!quit)
        {
            while (SDL.SDL_PollEvent(out e) == 1)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    quit = true;
                }
            }

            SDL.SDL_Delay(16); // ~60 FPS
        }

        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
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
    byte[] stack = new byte[16];
    private byte sp; //stack pointer
    byte[] key = new byte[16]; //keeps the current state of the key
    
    

    private const uint fontSize = 80;
    byte[] fontSet =
    [
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
    ];

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
    }

    void LoadFont()
    {
        for (long i = 0; i < fontSize; i++)
        {
            memory[0x50 + i] = fontSet[i]; //load font from the font start address
        }
    }
    
    
    
    
    
    //note that Vx is a stand-in for our registers, where x is 0-F
    
    public void CLS(){ Array.Clear(gfx); } //CLS clear display
    public void RET(){} //RET return from routine
    public void JUMP(ushort addr){pc = addr;} //JP, jump to location nnn
    public void CALL(){}
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
    public void SUB_XY(){} //subtract two registers,
    
}