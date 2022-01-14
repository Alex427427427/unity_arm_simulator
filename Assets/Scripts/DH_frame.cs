using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = System.Math;
using OperationModes = main.OperationModes;

public class DH_frame : MonoBehaviour
{
    // selecting the mode alters beaviour of dh frames.
    public OperationModes operation_mode = OperationModes.manual_joint_space_control;

    // whether it's base frame
    public bool is_base_frame;

    // parent frame
    public DH_frame parent;

    // DH parameters
    [Header("DH parameters")]
    public float link_length;
    public float link_twist;
    public float link_offset;

    // joint state
    [Range(-180, 180)]
    public float joint_angle = 0;

    //transform matrices, positions, rotations, in right handed frames, and then left-adjust them
    //NOTE: ALL standard right handed matrices M will need to be adjusted for left handed system by calling 
    // rotation_wrapper(M) before applying transformation! Do no more, no less!
    //We will store the right handed forms due to ROS data (and the entire rest of the world) being right handed.
    public Matrix4x4 T_local; // currently not being used; a new T_local has to be obtained via get_T_local() anyway. 
    public Matrix4x4 T_global;
    public Matrix4x4 LH_adjusted_T_global;
    private Vector3 global_pos;
    private Quaternion global_rotation;


    void Start() 
    {
        // if base frame, identity matrix (then converted to left handed axes later)
        if (is_base_frame)
        {
            T_global = Matrix4x4.identity;
        }
    }

    void FixedUpdate()
    {
        // only need to do anything if it's not the base frame
        if (!is_base_frame)
        {
            // only need to calculate if in manual mode, play mode, or recording mode.
            if (operation_mode == OperationModes.manual_joint_space_control || operation_mode == OperationModes.play_mode || operation_mode == OperationModes.recording)
            {
                // update global transform
                T_global = parent.get_T_global() * get_T_local();
                
                // assign T_global to transform
                assign_transform();
            }
        }
    }


    // convenience related functions
    // calculate T_global based on parents 
    Matrix4x4 get_T_global()
    {
        if (is_base_frame)
        {
            return T_global;
        } 
        else 
        {
            return parent.get_T_global() * get_T_local();
        }
    }

    // calculate T_local, in right handed axes system, due to the FK and IK maths being worked out in right handed system.
    // derived from forward kinematics, in DH frame convention
    Matrix4x4 get_T_local()
    {
        Matrix4x4 M = new Matrix4x4();
        M.SetColumn(0, new Vector4(cosd(joint_angle),  sind(joint_angle)*cosd(link_twist), sind(joint_angle)*sind(link_twist), 0)); 
        M.SetColumn(1, new Vector4(-sind(joint_angle), cosd(joint_angle)*cosd(link_twist), cosd(joint_angle)*sind(link_twist), 0)); 
        M.SetColumn(2, new Vector4(0,                  -sind(link_twist),                  cosd(link_twist),                   0)); 
        M.SetColumn(3, new Vector4(link_length,        -link_offset*sind(link_twist),      link_offset*cosd(link_twist),       1)); 

        return M;
    }

    // assigns the transform according to T_global
    void assign_transform()
    {
        LH_adjusted_T_global = rotation_wrapper(T_global);

        // extract position and orientation from global transform
        global_pos = new Vector3(LH_adjusted_T_global[0, 3], LH_adjusted_T_global[1, 3], LH_adjusted_T_global[2, 3]);
        global_rotation = to_quaternion(rotation_wrapper(T_global)); //orientation must be extracted from the right handed version of the matrix.
        
        // assign to transform
        transform.position = global_pos;
        transform.rotation = global_rotation;
    }


    // maths related functions
    // sin and cos in degrees
    float sind(float degree)
    {
        float angle = (float)Mathf.Deg2Rad * degree;
        float sine_out = (float)Math.Sin(angle);
        return sine_out;
    }

    float cosd(float degree)
    {
        float angle = (float)Mathf.Deg2Rad * degree;
        float cos_out = (float)Math.Cos(angle);
        return cos_out;
    }

    // converts Transformation matrices between left and right hand systems
    // correct rotation is constructed by pre-and-post multiplying a conversion matrix between left and right handed axes
    Matrix4x4 rotation_wrapper(Matrix4x4 M)
    {
        Matrix4x4 J = new Matrix4x4();
        J.SetRow(0, new Vector4(1, 0, 0, 0));
        J.SetRow(1, new Vector4(0, 0, 1, 0));
        J.SetRow(2, new Vector4(0, 1, 0, 0));
        J.SetRow(3, new Vector4(0, 0, 0, 1));

        return J * M * J;
    }

    // constructs quaternions from t_matrix
    // NOTE: if input is a left handed matrix, output is a left handed quaternion (what we want for unity), if input is a right handed matrix, output is a right handed quaternion.
    Quaternion to_quaternion(Matrix4x4 T_matrix)
    {
        Quaternion q = new Quaternion(0, 0, 0, 0);

        float tr = T_matrix[0, 0] + T_matrix[1, 1] + T_matrix[2, 2];
        if (tr > 0)
        {
            float S = (float)Math.Sqrt(tr + 1.0) * 2; // S=4*qw 
            q.w = (float)0.25 * S;
            q.x = (T_matrix[2, 1] - T_matrix[1, 2]) / S;
            q.y = (T_matrix[0, 2] - T_matrix[2, 0]) / S;
            q.z = (T_matrix[1, 0] - T_matrix[0, 1]) / S;
            return q;
        } 
        else if ((T_matrix[0, 0] > T_matrix[1, 1]) && (T_matrix[0, 0] > T_matrix[2, 2])) 
        {
            float S = (float)Math.Sqrt(1.0 + T_matrix[0, 0] - T_matrix[1, 1] - T_matrix[2, 2]) * 2; // S=4*qx 
            q.w = (T_matrix[2, 1] - T_matrix[1, 2]) / S;
            q.x = (float)0.25 * S;
            q.y = (T_matrix[0, 1] + T_matrix[1, 0]) / S;
            q.z = (T_matrix[0, 2] + T_matrix[2, 0]) / S;
            return q;
        }
        else if (T_matrix[1, 1] > T_matrix[2, 2])
        {
            float S = (float)Math.Sqrt(1.0 + T_matrix[1, 1] - T_matrix[0, 0] - T_matrix[2, 2]) * 2; // S=4*qy
            q.w = (T_matrix[0, 2] - T_matrix[2, 0]) / S;
            q.x = (T_matrix[0, 1] + T_matrix[1, 0]) / S;
            q.y = (float)0.25 * S;
            q.z = (T_matrix[1, 2] + T_matrix[2, 1]) / S;
            return q;
        }
        else 
        {
            float S = (float)Math.Sqrt(1.0 + T_matrix[2, 2] - T_matrix[0, 0] - T_matrix[1, 1]) * 2; // S=4*qz
            q.w = (T_matrix[1, 0] - T_matrix[0, 1]) / S;
            q.x = (T_matrix[0, 2] + T_matrix[2, 0]) / S;
            q.y = (T_matrix[1, 2] + T_matrix[2, 1]) / S;
            q.z = (float)0.25 * S;
            return q;
        }
    }
}