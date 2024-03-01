import bvh
import xml.etree.ElementTree as ET


def convert(ascii_file, vsk_file, output_file=r'converted.bvh'):
    # Parse the XML from the .vsk file
    tree = ET.parse(vsk_file)
    root = tree.getroot()

    # Open the .bvh file for writing
    with open(output_file, 'w') as bvh:
        # Write the header information for the .bvh file
        bvh.write("HIERARCHY\n")
        bvh.write("ROOT {0}\n".format(root.find('Skeleton/Segment').attrib['NAME']))
        bvh.write("{\n")

        # Recursive function to write segments and joints
        def write_segment(segment):
            bvh.write("\t" * depth + "OFFSET {0} {1} {2}\n".format(*map(float, find_joint(segment).attrib['PRE-POSITION'].split())))
            bvh.write("\t" * depth + "CHANNELS 3 {0}\n".format(" ".join(segment.attrib['T'].split())))
            for child in segment.findall("Segment"):
                bvh.write("\t" * (depth + 1) + "JOINT {0}\n".format(child.attrib['NAME']))
                bvh.write("\t" * (depth + 1) + "{\n")
                write_segment(child)
                bvh.write("\t" * (depth + 1) + "}\n")
            for child in segment.findall("JointFree"):
                bvh.write("\t" * (depth + 1) + "JOINT {0}\n".format(child.attrib['NAME']))
                bvh.write("\t" * (depth + 1) + "{\n")
                write_segment(child)
                bvh.write("\t" * (depth + 1) + "}\n")

        # Start writing segments and joints recursively
        depth = 1
        write_segment(root.find("Skeleton/Segment"))

        # End of the hierarchy
        bvh.write("}\n")


def find_joint(segment_element):
    # Iterate over the child elements of the segment
    for child in segment_element:
        # Check if the child element is a joint
        if child.tag.startswith("Joint"):
            return child  # Return the first joint found
    return None  # Return None if no joint is found


if __name__ == '__main__':
    ascii_file = r'data/level_obs_big_arch_01.csv'
    vsk_file = r'data/DanielDay01.vsk'
    convert(ascii_file, vsk_file)