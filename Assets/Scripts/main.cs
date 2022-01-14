using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = System.Math;
using System.IO;

// hi i was here

public class main : MonoBehaviour
{
    // mode of operation
    public enum OperationModes {
        manual_joint_space_control,
        play_mode,
        recording,
        control_via_ros
    };
    public OperationModes operation_mode = OperationModes.manual_joint_space_control;
    private int ros_frequency = 5; // rough estimate of ros publishing frequency
    
    // dh frames
    public int number_of_frames = 7;
    public DH_frame base_frame;
    public DH_frame end_effector_frame;
    public float[] initial_angles = {0, 0, -179, -158, -20, -92, 0}; // where ith value is the initial joint angle of the ith joint
    private DH_frame[] DH_frames;

    // frame keeping
    private uint frame = 0;

    // ros data
    public int record_duration = 20; // how many seconds does the recorder run
    private int record_frequency = 5; // want to simulate 5 hz ros messages

    private string recording_file_path = "Assets/Resources/recorder_data.txt";
    private string ros_file_path = "Assets/Resources/ros_data.txt";
    private static StreamWriter sr;
    private string output = "";
    private float ros_to_unity_scale = 0.01f; // ros messages are in mm; unity is currently on dm.

    void Awake()
    {
        construct_DH_frame_array();
    }

    void Start()
    {
        // assign initial angles
        for (int i = 0; i < number_of_frames; i++)
        {
            DH_frames[i].joint_angle = initial_angles[i];
        }

        // set frame rates
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        switch (operation_mode)
        {
            case OperationModes.manual_joint_space_control:
                Application.targetFrameRate = ros_frequency; // normal manual control uses 5 hz to simulate ros message frequency
                break;
            case OperationModes.play_mode:
                Application.targetFrameRate = 50; // play mode uses higher speed
                break;
            case OperationModes.recording:
                Application.targetFrameRate = ros_frequency; // normal manual control uses 5 hz to simulate ros message frequency
                sr = File.CreateText(recording_file_path); // create file for recording
                break;
            case OperationModes.control_via_ros:
                Application.targetFrameRate = ros_frequency; // ros mode simulates 5hz frequency
                break;
            default:
                Application.targetFrameRate = ros_frequency; // normal manual control uses 5 hz to simulate ros message frequency
                break;
        }
    }

    void Update()
    {
        frame++;    
        switch (operation_mode)
        {
            case OperationModes.manual_joint_space_control:
                update_joint_angles_with_keyboard();
                break;

            case OperationModes.play_mode:
                update_joint_angles_with_keyboard();
                break;

            case OperationModes.recording:
                update_joint_angles_with_keyboard();
                append_pose_to_output();
                // if not at the end of the recording, separate each state with "\n".
                if (frame < record_duration * record_frequency)
                {
                    output += "\n";
                }
                else // if time limit reached, close file.
                {
                    sr.WriteLine(output);
                    sr.Close();
                }
                break;

            case OperationModes.control_via_ros:
                set_config_with_ros_messages();
                break;

            default:
                break;
        }   
    }


    // FUNCTIONS
    // constructs array of DH_frames at void Start()
    void construct_DH_frame_array()
    {
        // construct array of dh frames
        DH_frames = new DH_frame[number_of_frames];
        // set final frame
        DH_frames[number_of_frames - 1] = end_effector_frame; 

        // for every frame starting from the end...
        for (int i = number_of_frames - 1; i > 0; i--) 
        {
            // add the parent
            DH_frames[i - 1] = DH_frames[i].parent; 

            // set modes
            DH_frames[i].is_base_frame = false;
            DH_frames[i].operation_mode = operation_mode;
        }

        // set mode of base frame
        DH_frames[0].is_base_frame = true;
    }

    // detects keyboard input, updates joint angles accordingly
    void update_joint_angles_with_keyboard()
    {
        // lower 3 joints, controlled by WASDQE. QE: J1; AD: J2; WS: J3
        if (Input.GetKey("q")) 
        {
            DH_frames[1].joint_angle += 1;
            print("Q pressed!");
        }
        else if (Input.GetKey("e")) 
        {
            DH_frames[1].joint_angle -= 1;
            print("E pressed!");
        }
        if (Input.GetKey("a")) 
        {
            DH_frames[2].joint_angle += 1;
            print("A pressed!");
        }
        else if (Input.GetKey("d")) 
        {
            DH_frames[2].joint_angle -= 1;
            print("D pressed!");
        }
        if (Input.GetKey("s")) 
        {
            DH_frames[3].joint_angle += 1;
            print("S pressed!");
        }
        else if (Input.GetKey("w")) 
        {
            DH_frames[3].joint_angle -= 1;
            print("W pressed!");
        }

        //upper 3 joints, controlled by IJKLUO. IK: J4; JL: J5; OU: J6
        if (Input.GetKey("k")) 
        {
            DH_frames[4].joint_angle += 1;
            print("K pressed!");
        }
        else if (Input.GetKey("i")) 
        {
            DH_frames[4].joint_angle -= 1;
            print("I pressed!");
        }
        if (Input.GetKey("j")) 
        {
            DH_frames[5].joint_angle += 1;
            print("J pressed!");
        }
        else if (Input.GetKey("l")) 
        {
            DH_frames[5].joint_angle -= 1;
            print("L pressed!");
        }
        if (Input.GetKey("o")) 
        {
            DH_frames[6].joint_angle += 1;
            print("O pressed!");
        }
        else if (Input.GetKey("u")) 
        {
            DH_frames[6].joint_angle -= 1;
            print("U pressed!");
        }
    }

    // appends all transformation matrices (in the current state, a snapshot) into main.output.
    // only needed when recording mode is active.
    void append_pose_to_output()
    {
        // for each dh frame...
        for (int n = 0; n < number_of_frames; n++)
        {
            for (int i = 0; i <= 3; i++) //for each row
            {
                for (int j = 0; j <= 3; j++) //for each column
                {
                    // for the translation vector, it has to be scaled.
                    if (j == 3 && i < 3)
                    {
                        output = output + (DH_frames[n].T_global[i,j] / ros_to_unity_scale).ToString();
                    }
                    else // else, just append data
                    {
                        output = output + DH_frames[n].T_global[i,j].ToString();
                    }
                    // if not at the end of the row, separate elements with " ".
                    if (j < 3) {output += " ";}
                }
                // if not at the end of the matrix, separate rows with ";".
                if (i < 3) {output += ";";}
            }
            // if not at the end of the DH_frames, separate matrices with ",".
            if (n < number_of_frames - 1) {output += ",";}
        }
    }

    // reads from a text file containing ros messages, and sets the transforms of the DH_frames.
    void set_config_with_ros_messages()
    {
        // read all lines. Each line is of the form: "x,y,z,qx,qy,qz,qw\n"
        string[] ros_data_lines = File.ReadAllLines(ros_file_path);

        // for each line (each dh frame)...
        for (int i = 0; i < number_of_frames; i++)
        {
            // split into 7 values; first 3 are translation, last 4 are quaternion
            string[] transform_strings = ros_data_lines[i].Split(',');
            float[] transform_values = new float[7];
            // for each value...
            for (int j = 0; j < 7; j++)
            {
                // store into array
                transform_values[j] = float.Parse(transform_strings[j]);
            }

            // assign transforms
            DH_frames[i].transform.position = new Vector3(
                transform_values[0] * ros_to_unity_scale, // x <- x
                transform_values[2] * ros_to_unity_scale, // y <- z
                transform_values[1] * ros_to_unity_scale // z <- y
            ); // y ans z swap to become left handed
            DH_frames[i].transform.rotation = new Quaternion(
                -1*transform_values[3], // x <- -x
                -1*transform_values[5], // y <- -z
                -1*transform_values[4], // z <- -y
                transform_values[6] // w <- w
            ); // have to find out how to convert into left handed quaternions. Flip y and z axes, then negate all 3.
        }
    }
}


