using System;
using System.IO;
using SDL2;

namespace CHIP_8_SDL;

public static class Program
{
   static IntPtr renderer;
   static IntPtr font;
   //static string romPath = @"C:\Users\zaidg\Downloads\test_opcode.ch8";
   static string romPath = "/home/zaid/Downloads/1-chip8-logo.ch8";
   private static int delay;

   private static int scale;
 


    public static void Main()
    {
        delay = int.Parse(Console.ReadLine());
        scale = (int.Parse(Console.ReadLine()) * 2);
        Console.WriteLine();
        
        
        
        CPU chip = new CPU();
        chip.debugLoadRom(romPath);
        DoEmulatorRender(chip, scale);
    }

    static void DoEmulatorRender(CPU chip, int scale)
    {
        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        IntPtr window = SDL.SDL_CreateWindow("Woah",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            64 * scale, 32 * scale,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

        renderer = SDL.SDL_CreateRenderer(window, -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
            SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC
        );

        var lastCycleTime = DateTime.Now;

        bool quit = false;
        SDL.SDL_Event e;

        while (!quit)
        {
            var currentTime = DateTime.Now;
            var dt = (currentTime - lastCycleTime);

            if (dt > TimeSpan.FromSeconds(delay))
            {
                Console.WriteLine(dt);
                lastCycleTime = currentTime;
                chip.CycleCPU();
            }

            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    quit = true;
            }

            // Clear screen
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);

            // Draw CHIP-8 pixels
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (chip.gfx[x, y] != 0)
                    {
                        SDL.SDL_Rect rect = new SDL.SDL_Rect
                        {
                            x = x * scale,
                            y = y * scale,
                            w = scale,
                            h = scale
                        };
                        SDL.SDL_RenderFillRect(renderer, ref rect);
                    }
                }
            }

            // Present backbuffer
            SDL.SDL_RenderPresent(renderer);

            SDL.SDL_Delay(16); // ~60 FPS
        }

        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
    }

    static void DoRender(CPU c, int scale)
    {
        // Initialize SDL and TTF
        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        SDL_ttf.TTF_Init();

        IntPtr window = SDL.SDL_CreateWindow(
            "That funky music, White Boy",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            64 * scale, 32 * scale,
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
            
            //RenderMemory(c, bytesPerLine, startX, startY, lineHeight);
            RenderScreen(c, startX, startY, lineHeight);

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
    public byte[,] gfx = new byte[64, 32]; //graphics representation, 2048 pixels
    private byte delayTimer;
    private byte soundTimer;
    ushort[] stack = new ushort[16];
    private byte sp; //stack pointer
    byte[] key = new byte[16]; //keeps the current state of the key
    
    
    private Dictionary<ConsoleKey, int> keyMap = new();
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
        initKeys();
        
    }
    
    
    private void initKeys() //maps keys to the index in the array
    {
        keyMap.Add(ConsoleKey.D1, 0);
        keyMap.Add(ConsoleKey.D2, 1);
        keyMap.Add(ConsoleKey.D3, 2);
        keyMap.Add(ConsoleKey.D4, 4);
        keyMap.Add(ConsoleKey.Q, 5);
        keyMap.Add(ConsoleKey.W, 6);
        keyMap.Add(ConsoleKey.E, 7);
        keyMap.Add(ConsoleKey.R, 8);
        keyMap.Add(ConsoleKey.A, 9);
        keyMap.Add(ConsoleKey.S, 10);
        keyMap.Add(ConsoleKey.D, 11);
        keyMap.Add(ConsoleKey.F, 12);
        keyMap.Add(ConsoleKey.Z, 13);
        keyMap.Add(ConsoleKey.X, 14);
        keyMap.Add(ConsoleKey.C, 15);
        keyMap.Add(ConsoleKey.V, 16);
    }
    void LoadROM(string filepath)//rom loader
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

    public void CycleCPU()
    {
        opCode = memory[pc];
        Console.WriteLine(pc.ToString(format: "X4"));
        pc += 2;
        DecodeOpcode(opCode);

        if (delayTimer > 0)
        {
            delayTimer--;
        }

        if (soundTimer == 0)
        {
            soundTimer--;
        }
        
        CheckKeyStatus();
    }

    void CheckKeyStatus()
    {
        if (Console.KeyAvailable)
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            key[keyMap[k.Key]] = 1;
        }
    }
    
    public void DecodeOpcode(ushort opcode)
    {
        string translatedOpCode = opcode.ToString("X4");
        Console.WriteLine(translatedOpCode); //for debugging
        string tcode;
        byte treg;
        byte treg2;
        byte tkk;
        //i could do fancy bitwise operations but im dumb, so abuse stringbuilder
        switch (translatedOpCode[0]) //i feel like yandev typing this out
        {
            case '0':
                if (translatedOpCode[3] == '0')
                {
                    CLS();
                }
                else if (translatedOpCode[3] == 'E')
                {
                    Console.WriteLine("RET");
                    RET();
                }
                break;
            case '1':
                tcode = translatedOpCode.Substring(1);
                JUMP(ushort.Parse(tcode));
                break;
            case '2':
                tcode = translatedOpCode.Substring(1);
                CALL(ushort.Parse(tcode));
                break;
            case '3':
                treg = byte.Parse(translatedOpCode[1].ToString());
                tkk = byte.Parse(translatedOpCode.Substring(2));
                SE_XKK(treg, tkk);
                break;
            case '4':
                treg = byte.Parse(translatedOpCode[1].ToString());
                tkk = byte.Parse(translatedOpCode.Substring(2));
                SNE_XKK(treg, tkk);
                break;
            case '5':
                treg = byte.Parse(translatedOpCode[1].ToString());
                treg2 = byte.Parse(translatedOpCode[2].ToString());
                SE_XY(treg, treg2);
                break;
            case '6':
                treg = byte.Parse(translatedOpCode[1].ToString());
                tkk = byte.Parse(translatedOpCode.Substring(2));
                LD_XKK(treg, tkk);
                break;
            case '7':
                treg = byte.Parse(translatedOpCode[1].ToString());
                tkk = byte.Parse(translatedOpCode.Substring(2));
                ADD_XKK(treg, tkk);
                break;
            case '8': //oh boy here we go
                switch (translatedOpCode[4]) //there is 1000% a better way of doing this, but this was funny
                {
                    case '0':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        LD_XY(treg, treg2);
                        break;
                    case '1':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        OR_XY(treg, treg2);
                        break;
                    case '2':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        AND_XY(treg, treg2);
                        break;
                    case '3':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        XOR_XY(treg, treg2);
                        break;
                    case '4':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        ADD_XY(treg, treg2);
                        break;
                    case '5':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        SUB_XY(treg, treg2);
                        break;
                    case '6':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        SHR_X(treg);
                        break;
                    case '7':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        treg2 = byte.Parse(translatedOpCode[2].ToString());
                        SUBN_XY(treg, treg2);
                        break;
                    case 'E':
                        treg = byte.Parse(translatedOpCode[1].ToString());
                        SHL_X(treg);
                        break;
                    default:
                        Console.WriteLine("How did this even happen???");
                        break;
                }
                break;
            case '9':
                treg = byte.Parse(translatedOpCode[1].ToString());
                treg2 = byte.Parse(translatedOpCode[2].ToString());
                SNE_XY(treg, treg2);
                break;
            case 'A':
                treg = byte.Parse(translatedOpCode[1].ToString());
                LD_IADR(treg);
                break;
            case 'B':
                treg = byte.Parse(translatedOpCode[1].ToString());
                JP_VADR(treg);
                break;
            case 'C':
                tcode = translatedOpCode.Substring(1);
                treg = byte.Parse(translatedOpCode[1].ToString());
                RND(treg,byte.Parse(tcode));
                break;
            case 'D':
                treg = byte.Parse(translatedOpCode[1].ToString());
                treg2 = byte.Parse(translatedOpCode[2].ToString());
                tcode = translatedOpCode[4].ToString();
                DRAW(treg,treg2, byte.Parse(tcode));
                break;
            case 'E':
                treg = byte.Parse(translatedOpCode[1].ToString());
                if (translatedOpCode[4] == 'E')
                {
                    SKP_X(treg);
                }
                else
                {
                    SKNP_X(treg);
                }
                break;
            case 'F': //here we go again
                treg = byte.Parse(translatedOpCode[1].ToString());
                if (translatedOpCode[3] == '0')
                {
                    if (translatedOpCode[4] == '7')
                    {
                        LD_XDT(treg);
                    }
                    else
                    {
                        LD_XKT(treg);
                    }
                }
                else if (translatedOpCode[3] == '1')
                {
                    if (translatedOpCode[4] == '5')
                    {
                        LD_DTX(treg);
                    }
                    else if (translatedOpCode[4] == '8')
                    {
                        LD_STX(treg);
                    }
                    else
                    {
                        ADD_IX(treg);
                    }
                }
                else if (translatedOpCode[3] == '2')
                {
                    LD_FX(treg);
                }
                else if (translatedOpCode[3] == '3')
                {
                    LD_BX(treg);
                }
                else if (translatedOpCode[3] == '5')
                {
                    LD_IX(treg);
                }
                else if (translatedOpCode[3] == '6')
                {
                    LD_XI(treg);
                }
                break;
            default:
                Console.WriteLine("Lol");
                break;
            
        }
    }

    //note that Vx is a stand-in for our registers, where x is 0-F
    
    public void CLS(){ Array.Clear(gfx); } //CLS clear display

    public void RET() //RET return from routine
    {
        Console.WriteLine("Pre Iterate: " + sp + " Post iteration: " + (sp-1));
        pc = stack[--sp];

    }

    public void JUMP(ushort addr)
    {
        pc = addr;
    } //JP, jump to location nnn

    public void CALL(ushort addr)
    {
        stack[++sp] = pc;
        pc = addr;
    }

    public void SE_XKK(byte reg, byte kk) //skip if vx = a byte
    {
        if (V[reg] == kk)
        {
            pc += 2;
        }
    } 

    public void SNE_XKK(byte reg, byte kk) //skip if vx != a byte
    {
        if (V[reg] != kk)
        {
            pc += 2;
        }
    }

    public void SE_XY(byte reg1, byte reg2)
    {
        if (V[reg1] == V[reg2])
        {
            pc += 2;
        }
    } //skip if vx = vy

    public void LD_XKK(byte reg, byte kk)
    {
        V[reg] = kk;
    } //sets vx to a byte

    public void ADD_XKK(byte reg, byte kk)
    {
        V[reg] = (byte)(V[reg] + kk);
    } //Vx = Vx + a byte

    public void LD_XY(byte reg1, byte reg2)
    {
        V[reg1] = V[reg2];
    } //set vx to vy

    public void OR_XY(byte reg1, byte reg2)
    {
        V[reg1] = (byte)(V[reg1] | V[reg2]); //c# bitwise or is |
    } //set vx to vx OR vy

    public void AND_XY(byte reg1, byte reg2)
    {
        V[reg1] = (byte)(V[reg1] & V[reg2]);
    } //set vx to vx AND vy

    public void XOR_XY(byte reg1, byte reg2)
    {
        V[reg1] = (byte)(V[reg1] ^ V[reg2]);
    } //set vx to vx XOR vy (exclusive or)

    public void ADD_XY(byte reg1, byte reg2)
    {
        if (V[reg1] + V[reg2] > 255)
        {
            V[15] = 1;
        }
        else
        {
            V[15] = 0;
        }
        V[reg1] = (byte)(V[reg1] + V[reg2]);
    } //add two registers, if greater than 255 VF is set to 1

    public void SUB_XY(byte reg1, byte reg2)
    {
        if (V[reg1] > V[reg2])
        {
            V[15] = 1;
        }
        else
        {
            V[15] = 0;
        }
        V[reg1] = (byte)(V[reg1] - V[reg2]);
    } //subtract two registers (vx-vy stored in vx), if vx > vy, vf is 1. otherwise 0

    public void SHR_X(byte reg)
    {
        V[15] = (byte)(V[reg] & 0x1u);
        V[reg] >>= 1;
    } //set vx to vx SHR 1

    public void SUBN_XY(byte reg1, byte reg2)
    {
        if (V[reg2] > V[reg1])
        {
            V[15] = 1;
        }
        else
        {
            V[15] = 0;
        }
        V[reg1] = (byte)(V[reg2] - V[reg1]);
    } //set vx = vy- vx, vf will NOT borrow

    public void SHL_X(byte reg)
    {
        V[15] = (byte)((V[reg] & 0x80u) >> 7);
        V[reg] <<= 1;
    } //set vx = vx SHL 1

    public void SNE_XY(byte reg1, byte reg2) //skip if vx != vy
    {
        if (V[reg1] != V[reg2])
        {
            pc += 2;
        }
    }

    public void LD_IADR(ushort addr) //sets the index register to an address
    {
        i = addr;
    }

    public void JP_VADR(ushort addr)
    {
        pc = (ushort)(addr + V[0]);
    }

    public void RND(byte reg, byte kk) //set vx to a random byte AND kk
    {
        Random rnd = new Random();
        byte r = (byte)rnd.Next(0, 256);
        V[reg] = (byte)(r & kk);
    }

    public void DRAW(byte reg1, byte reg2, byte h)
    {
        byte xPos = (byte)(V[reg1] % 64);
        byte yPos = (byte)(V[reg2] % 32);
        V[15] = 0;

        for (int row = 0; row < h; row++)
        {
            byte spriteByte = memory[i + row];

            for (int col = 0; col < 8; col++)
            {
                byte spritePixel = (byte)(spriteByte & (0x80u >> col));

                if (spritePixel != 0)
                {
                    int px = (xPos + col) % 64;
                    int py = (yPos + row) % 32;

                    //collision
                    if (gfx[py, px] == 1)
                    {
                        V[15] = 1;
                    }

                    //XOR toggle
                    gfx[py, px] ^= 1;
                }
            }
        }

    }

    public void SKP_X(byte reg)
    {
        byte k = V[reg];
        if (key[k] != 0)
        {
            pc += 2;
        }
    }
    public void SKNP_X(byte reg)
    {
        byte k = V[reg];
        if (key[k] == 0)
        {
            pc += 2;
        }
    }

    public void LD_XDT(byte reg)
    {
        V[reg] = delayTimer;
    }
    
    public void LD_XKT(byte reg) //key press
    {
        for (byte j = 0; j < 16; j++)
        {
            if (key[j] != 0)
            {
                V[reg] = j;
                return;
            }
        }

        pc -= 2;
    }

    public void LD_DTX(byte reg)
    {
        delayTimer = V[reg];
    }

    public void LD_STX(byte reg)
    {
        soundTimer =  V[reg];
    }

    public void ADD_IX(byte reg)
    {
        i += (byte)(i + V[reg]);
    }

    public void LD_FX(byte reg)
    {
        i = (ushort)(0x50 + (5 * V[reg]));
    }

    public void LD_BX(byte reg)
    {
        byte value = V[reg];
        
        //ones
        memory[i + 2] = (byte)(value % 10);
        value /= 10;

        //tens
        memory[i + 1] = (byte)(value % 10);
        value /= 10;

        //hunded
        memory[i] = (byte)(value % 10);
    }

    public void LD_XI(byte reg) //copy the values of every register into memory starting at I
    {
        for (byte j = 0; j < reg; j++)
        {
            memory[i+j] = V[reg];
        }
    }

    public void LD_IX(byte reg)
    {
        for (byte j = 0; j < reg; j++)
        {
            V[j] = memory[i+j];
        }
    }
}