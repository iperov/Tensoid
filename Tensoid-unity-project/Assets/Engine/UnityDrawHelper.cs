using System;
using UnityEngine;

public class UnityDrawHelper
{
	public UnityDrawHelper ()
	{
	}

	static public void drawLine2D ( Vector3 A, Vector3 B, float line_width, Color color_from, Color color_to  )
	{
		Vector3 AB = B-A;

		Vector3 AB_p1 = Vector3.Cross ( AB, new Vector3(0,0,-1.0f) );
				AB_p1.Normalize();

		Vector3 p1 = A + AB_p1*(line_width/2);
		Vector3 p2 = B + AB_p1*(line_width/2);
		Vector3 p3 = B - AB_p1*(line_width/2);
		Vector3 p4 = A - AB_p1*(line_width/2);

		GL.Begin(GL.QUADS);

		GL.Color(color_from);
		GL.Vertex(p4);
		GL.Vertex(p1);

		GL.Color(color_to);
		GL.Vertex(p2);
		GL.Vertex(p3);

		GL.End();
	}

	static public void drawLine2D ( Vector3 A, Vector3 B, Vector3 C_scissor, float line_width, Color color_from, Color color_to  )
	{
		Vector3 AB = B-A;

		Vector3 A_A1_vec = Vector3.Cross ( AB, Vector3.back );
				A_A1_vec.Normalize();

		Vector3 CA_vec = (A-C_scissor).normalized;

		float line_width_half = line_width/2;

		double cos_a = Vector3.Dot (A_A1_vec, CA_vec) / ( A_A1_vec.magnitude * CA_vec.magnitude );

		Vector3 A11 = A + CA_vec*( line_width_half / (float)cos_a );
		Vector3 A22 = A - CA_vec*( line_width_half / (float)cos_a );

		Vector3 CB_vec = (B-C_scissor).normalized;
				cos_a = Vector3.Dot (A_A1_vec, CB_vec) / ( A_A1_vec.magnitude * CB_vec.magnitude );

		Vector3 B11 = B + CB_vec*( line_width_half / (float)cos_a );
		Vector3 B22 = B - CB_vec*( line_width_half / (float)cos_a );

		GL.Begin(GL.TRIANGLE_STRIP);

		GL.Color(color_from);
		GL.Vertex(A22);
		GL.Vertex(A11);

		GL.Color(color_to);
		GL.Vertex(B22);
		GL.Vertex(B11);

		GL.End();
	}

	static public void drawCircle2D ( Vector3 C, float radius, int nsections, float line_width, Color color )
	{
		double angle = Math.Atan2 (1.0,1.0)*8 / nsections;
		Vector3 point1 = new Vector3();
		Vector3 point2 = new Vector3();

		for (int i=0; i< nsections; ++i)
		{

			point1.Set ( C.x + (float) (radius * Math.Sin( i*angle )),
					 	 C.y + (float) (radius * Math.Cos( i*angle )), 0 );

			point2.Set ( C.x + (float) (radius * Math.Sin( ( (i+1)%nsections )*angle )),
					 	 C.y + (float) (radius * Math.Cos( ( (i+1)%nsections )*angle )), 0 );

			drawLine2D ( point1, point2, C, line_width, color, color );
		}
	}

	static public void drawPlus2D ( Vector3 C, float radius, float line_width, Color color )
	{
		drawLine2D ( C+Vector3.left*radius, C+Vector3.right*radius, line_width, color, color );
		drawLine2D ( C+Vector3.up*radius, C+Vector3.down*radius, line_width, color, color );
	}

	static public void drawMinus2D ( Vector3 C, float radius, float line_width, Color color )
	{
		drawLine2D ( C+Vector3.left*radius, C+Vector3.right*radius, line_width, color, color );
	}
}


