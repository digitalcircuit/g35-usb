/*
 *
 *+  Copyright (c) 2009 Ian Halpern
 *@  http://impulse.ian-halpern.com
 *
 *   This file is part of Impulse.
 *
 *   Impulse is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Impulse is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Impulse.  If not, see <http://www.gnu.org/licenses/>.
 */


#include "impulse.h"
#include <stdio.h>

int main() {

	int i = 0;
	long c = 0;
	im_start();

	int iterations, inner_iterations;
	for (iterations = 0; iterations < 100; ++iterations) {
		// Allow for the start-up delay of connecting to PulseAudio
		usleep(1*1000000 / 30);
		for (inner_iterations = 0; inner_iterations < 100; ++inner_iterations) {
			double *array = im_getSnapshot(IM_FFT);
			printf("%08lx: ", c++);
			for (i = 0; i < 256; i+=32)
				printf(" %.2f", array[i]);
			printf("\n");
			fflush(stdout);
		}
		im_stop();
		im_start();
	}
	im_stop();

	return 0;
}
